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

            // Stats
            var bonus = _world.Has<CombatStatsComponent>(attacker) ? _world.Get<CombatStatsComponent>(attacker).BaseAttackBonus : 0;
            var ac = _world.Has<CombatStatsComponent>(target) ? _world.Get<CombatStatsComponent>(target).ArmorClass : 10;

            int roll = _diceRoller.Roll(20);
            int total = roll + bonus;

            if (total >= ac)
            {
                int damage = _diceRoller.Roll(6);
                ref var vitals = ref _world.Get<VitalsComponent>(target);
                vitals.CurrentHP -= damage;

                Broadcast(combat, $"{attackerName} hits {targetName} for {damage} damage! ({total} vs AC {ac})");

                if (vitals.CurrentHP <= 0)
                {
                    Broadcast(combat, $"{targetName} has been defeated!");

                    // Check if it's a Player (has Output)
                    if (_world.Has<OutputMessageComponent>(target))
                    {
                        Broadcast(combat, $"{targetName} falls unconscious!");
                        _world.Remove<InCombatComponent>(target);

                        // --- NEW: Apply Status Tag ---
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
            }
            else
            {
                Broadcast(combat, $"{attackerName} attacks {targetName} and misses. ({total} vs AC {ac})");
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