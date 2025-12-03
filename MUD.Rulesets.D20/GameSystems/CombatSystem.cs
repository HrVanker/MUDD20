using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Linq;

namespace MUD.Rulesets.D20.GameSystems
{
    public class CombatSystem : ISystem<GameTime>
    {
        private readonly World _world;
        private readonly IDiceRoller _diceRoller;

        public CombatSystem(World world, GameState gameState, IDiceRoller diceRoller)
        {
            _world = world;
            _diceRoller = diceRoller;
        }

        private void SendMessage(Entity entity, string message)
        {
            if (_world.Has<OutputMessageComponent>(entity))
            {
                _world.Get<OutputMessageComponent>(entity).Messages.Add(message);
            }
        }

        private void Broadcast(CombatTurnComponent combat, string message)
        {
            Console.WriteLine($"[Combat] {message}");
            foreach (var participant in combat.TurnOrder)
            {
                if (_world.IsAlive(participant))
                    SendMessage(participant, message);
            }
        }

        public void Update(in GameTime gameTime)
        {
            var combatQuery = new QueryDescription().WithAll<CombatTurnComponent>();
            var combatEntity = Entity.Null;
            _world.Query(in combatQuery, (Entity entity) => { combatEntity = entity; });

            if (combatEntity == Entity.Null) return;

            ref var combat = ref _world.Get<CombatTurnComponent>(combatEntity);

            // 1. Cleanup Dead Combatants
            for (int i = combat.TurnOrder.Count - 1; i >= 0; i--)
            {
                if (!_world.IsAlive(combat.TurnOrder[i]))
                {
                    combat.TurnOrder.RemoveAt(i);
                    if (combat.CurrentTurnIndex >= i && combat.CurrentTurnIndex > 0)
                        combat.CurrentTurnIndex--;
                }
            }

            // 2. Check Win Condition
            if (combat.TurnOrder.Count <= 1)
            {
                Broadcast(combat, "\n--- COMBAT OVER! ---");
                foreach (var e in combat.TurnOrder) _world.Remove<InCombatComponent>(e);
                _world.Destroy(combatEntity);
                return;
            }

            var attackerEntity = combat.TurnOrder[combat.CurrentTurnIndex];

            // 3. PLAYER LOGIC: Wait for Input
            if (_world.Has<OutputMessageComponent>(attackerEntity))
            {
                // If the player has NOT queued an attack action, we return.
                // This effectively pauses the combat system until the player types 'attack'.
                if (!_world.Has<AttackActionComponent>(attackerEntity))
                {
                    // Optional: You could send a "It is your turn >" prompt here, 
                    // but be careful not to spam it 60 times a second.
                    return;
                }
            }
            // 4. NPC LOGIC: Auto-Attack
            else
            {
                if (!_world.Has<AttackActionComponent>(attackerEntity))
                {
                    var target = combat.TurnOrder.FirstOrDefault(e => e != attackerEntity && _world.IsAlive(e));
                    if (target != Entity.Null)
                    {
                        _world.Add(attackerEntity, new AttackActionComponent { Target = target });
                    }
                    else
                    {
                        AdvanceTurn(ref combat);
                        return;
                    }
                }
            }

            // 5. Execute Action
            if (_world.Has<AttackActionComponent>(attackerEntity))
            {
                ResolveAttack(attackerEntity, ref combat);
                AdvanceTurn(ref combat);
            }
        }

        private void ResolveAttack(Entity attacker, ref CombatTurnComponent combat)
        {
            var action = _world.Get<AttackActionComponent>(attacker);
            var target = action.Target;
            _world.Remove<AttackActionComponent>(attacker); // Consume the action

            if (!_world.IsAlive(target)) return;

            var attackerName = _world.Get<NameComponent>(attacker).Name;
            var targetName = _world.Get<NameComponent>(target).Name;

            // --- 1. ATTACK ROLL ---
            int d20 = _diceRoller.Roll(20);
            int bab = _world.Has<CombatStatsComponent>(attacker) ? _world.Get<CombatStatsComponent>(attacker).BaseAttackBonus : 0;

            // Calculate Strength Modifier (Melee default)
            int strMod = 0;
            if (_world.Has<CoreStatsComponent>(attacker))
            {
                var stats = _world.Get<CoreStatsComponent>(attacker);
                strMod = D20Rules.GetAbilityModifier(stats.Strength);
            }

            int totalAttack = d20 + bab + strMod;

            // --- 2. DEFENSE CALCULATION (Dynamic AC) ---
            int armorClass = CalculateArmorClass(target);

            // --- 3. RESOLUTION ---
            // Natural 1 is ALWAYS a MISS
            if (d20 == 1)
            {
                Broadcast(combat, $"{attackerName} attacks {targetName} but fumbles! (Natural 1)");
                return;
            }

            // Natural 20 is ALWAYS a HIT (Critical Hit logic can go here later)
            bool isHit = (d20 == 20) || (totalAttack >= armorClass);

            if (isHit)
            {
                // Calculate Damage (Weapon + Str)
                int damage = CalculateDamage(attacker) + strMod;
                if (damage < 1) damage = 1; // Minimum 1 damage

                // Apply Damage
                if (_world.Has<VitalsComponent>(target))
                {
                    var vitals = _world.Get<VitalsComponent>(target); // Get Copy
                    vitals.CurrentHP -= damage;
                    _world.Set(target, vitals); // Set Back

                    Broadcast(combat, $"{attackerName} hits {targetName} for {damage} damage! ({totalAttack} vs AC {armorClass})");

                    // Death Check
                    if (vitals.CurrentHP <= 0)
                    {
                        HandleDeath(target, targetName, ref combat);
                    }
                }
            }
            else
            {
                Broadcast(combat, $"{attackerName} attacks {targetName} and misses. ({totalAttack} vs AC {armorClass})");
            }
        }

        // --- HELPER: Dynamic AC Calculation ---
        private int CalculateArmorClass(Entity target)
        {
            int baseAC = 10;
            int armorBonus = 0;
            int shieldBonus = 0;
            int dexMod = 0;

            // 1. Get Dexterity
            if (_world.Has<CoreStatsComponent>(target))
            {
                var stats = _world.Get<CoreStatsComponent>(target);
                dexMod = D20Rules.GetAbilityModifier(stats.Dexterity);
            }

            // 2. Check Equipment for Armor/Shields
            if (_world.Has<EquipmentComponent>(target))
            {
                var equipment = _world.Get<EquipmentComponent>(target);

                // Check Armor Slot
                if (_world.IsAlive(equipment.Armor) && _world.Has<ArmorComponent>(equipment.Armor))
                {
                    var armor = _world.Get<ArmorComponent>(equipment.Armor);
                    armorBonus += armor.ArmorBonus;

                    // Cap Dex bonus if wearing armor
                    if (dexMod > armor.MaxDexBonus) dexMod = armor.MaxDexBonus;
                }

                // Check OffHand (Shield)
                if (_world.IsAlive(equipment.OffHand) && _world.Has<ArmorComponent>(equipment.OffHand))
                {
                    var shield = _world.Get<ArmorComponent>(equipment.OffHand);
                    shieldBonus += shield.ArmorBonus;
                }
            }

            // 3. Add Natural Armor (from CombatStats if any)
            // We treat the static 'ArmorClass' in CombatStats as "Natural Armor" or "Monster Base AC" for now.
            if (_world.Has<CombatStatsComponent>(target))
            {
                var combatStats = _world.Get<CombatStatsComponent>(target);
                baseAC += combatStats.NaturalArmor; // FIX: Use new field name
            }

            return baseAC + dexMod + armorBonus + shieldBonus;
        }

        // --- HELPER: Damage Calculation ---
        private int CalculateDamage(Entity attacker)
        {
            // Default Unarmed Damage
            int damage = 1;

            if (_world.Has<EquipmentComponent>(attacker))
            {
                var equip = _world.Get<EquipmentComponent>(attacker);
                if (_world.IsAlive(equip.MainHand) && _world.Has<WeaponComponent>(equip.MainHand))
                {
                    var weapon = _world.Get<WeaponComponent>(equip.MainHand);
                    damage = _diceRoller.Roll(weapon.DamageSides); // e.g., Roll(8) for Longsword
                    // Handle XdY (multiple dice) if you add 'DamageDice' count to WeaponComponent
                    for (int i = 1; i < weapon.DamageDice; i++)
                    {
                        damage += _diceRoller.Roll(weapon.DamageSides);
                    }
                }
            }
            return damage;
        }

        private void HandleDeath(Entity target, string targetName, ref CombatTurnComponent combat)
        {
            Broadcast(combat, $"{targetName} has been defeated!");

            // Check if it's a Player (has Output)
            if (_world.Has<OutputMessageComponent>(target))
            {
                Broadcast(combat, $"{targetName} falls unconscious!");
                _world.Remove<InCombatComponent>(target);

                if (!_world.Has<UnconsciousComponent>(target))
                    _world.Add(target, new UnconsciousComponent());
            }
            else
            {
                // NPCs Die
                _world.Remove<InCombatComponent>(target);
                _world.Destroy(target);
            }
        }

        private void AdvanceTurn(ref CombatTurnComponent combat)
        {
            combat.CurrentTurnIndex++;
            if (combat.CurrentTurnIndex >= combat.TurnOrder.Count)
            {
                combat.CurrentTurnIndex = 0;
                combat.RoundNumber++;
                Broadcast(combat, "--- Next Round ---");
            }
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}