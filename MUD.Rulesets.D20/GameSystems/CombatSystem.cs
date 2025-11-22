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


        public void Update(in GameTime gameTime)
        {
            var combatQuery = new QueryDescription().WithAll<CombatTurnComponent>();
            var combatEntity = Entity.Null;
            _world.Query(in combatQuery, (Entity entity) => { combatEntity = entity; });

            if (combatEntity == Entity.Null) return;

            ref var combat = ref _world.Get<CombatTurnComponent>(combatEntity);

            // --- NEW LOGIC: Check for end of combat ---
            var livingCombatants = combat.TurnOrder.Where(e => _world.IsAlive(e)).ToList();
            if (livingCombatants.Count <= 1)
            {
                var winner = livingCombatants.FirstOrDefault();
                if (winner != Entity.Null)
                {
                    var winnerName = _world.Get<NameComponent>(winner).Name;
                    Console.WriteLine($"\n--- COMBAT OVER! {winnerName} is victorious! ---");
                    // Remove the InCombat component from the winner
                    _world.Remove<InCombatComponent>(winner);
                }
                else
                {
                    Console.WriteLine("\n--- COMBAT OVER! All combatants were defeated. ---");
                }

                // Destroy the combat state entity to end the combat loop
                _world.Destroy(combatEntity);
                return;
            }
            // --- END NEW LOGIC ---

            var attackerEntity = combat.TurnOrder[combat.CurrentTurnIndex];

            if (!_world.IsAlive(attackerEntity))
            {
                AdvanceTurn(ref combat);
                return;
            }

            var attackerName = _world.Get<NameComponent>(attackerEntity).Name;
            Console.WriteLine($"\n--- {attackerName}'s Turn ---");

            if (!_world.Has<AttackActionComponent>(attackerEntity))
            {
                var target = combat.TurnOrder.FirstOrDefault(e => e != attackerEntity && _world.IsAlive(e));
                if (target != Entity.Null)
                {
                    _world.Add(attackerEntity, new AttackActionComponent { Target = target });
                }
            }

            if (_world.Has<AttackActionComponent>(attackerEntity))
            {
                ResolveAttack(attackerEntity, ref combat);
            }

            AdvanceTurn(ref combat);
        }

        private void ResolveAttack(Entity attacker, ref CombatTurnComponent combat)
        {
            var action = _world.Get<AttackActionComponent>(attacker);
            var target = action.Target;

            // Ensure the target is still alive and in combat.
            if (!_world.IsAlive(target) || !_world.Has<InCombatComponent>(target))
            {
                Console.WriteLine("  Target is invalid or has fled!");
                _world.Remove<AttackActionComponent>(attacker);
                return;
            }

            var attackerName = _world.Get<NameComponent>(attacker).Name;
            var targetName = _world.Get<NameComponent>(target).Name;
            var attackerCombatStats = _world.Get<CombatStatsComponent>(attacker);
            var targetCombatStats = _world.Get<CombatStatsComponent>(target);

            // 1. Attack Roll
            int attackRoll = _diceRoller.Roll(20);
            int totalAttack = attackRoll + attackerCombatStats.BaseAttackBonus;

            Console.WriteLine($"  {attackerName} attacks {targetName}!");
            Console.WriteLine($"  Attack Roll: {attackRoll} + BAB({attackerCombatStats.BaseAttackBonus}) = {totalAttack} vs AC {targetCombatStats.ArmorClass}");

            // 2. Compare to Armor Class
            if (totalAttack >= targetCombatStats.ArmorClass)
            {
                Console.WriteLine("  It's a HIT!");
                // 3. Calculate and Apply Damage
                int damage = _diceRoller.Roll(20); // Simulate a 1d6 weapon for now
                ref var targetVitals = ref _world.Get<VitalsComponent>(target);
                targetVitals.CurrentHP -= damage;

                Console.WriteLine($"  {targetName} takes {damage} damage, leaving them at {targetVitals.CurrentHP} HP.");

                // 4. Handle Entity Death
                if (targetVitals.CurrentHP <= 0)
                {
                    Console.WriteLine($"  {targetName} has been defeated!");
                    _world.Remove<InCombatComponent>(target); // Remove from combat
                    _world.Destroy(target); // Remove from the world entirely
                }
            }
            else
            {
                Console.WriteLine("  It's a MISS!");
            }

            // Remove the action component now that it has been processed.
            _world.Remove<AttackActionComponent>(attacker);
        }

        private void AdvanceTurn(ref CombatTurnComponent combat)
        {
            combat.CurrentTurnIndex++;
            if (combat.CurrentTurnIndex >= combat.TurnOrder.Count)
            {
                combat.CurrentTurnIndex = 0; // Loop back to the start of the order.
                Console.WriteLine("--- Next Round ---");
            }
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }

}