using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Linq;
using System.Collections.Generic;

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

        // Helper to send a message to a specific entity if they have a client attached
        private void SendMessage(Entity entity, string message)
        {
            if (_world.Has<OutputMessageComponent>(entity))
            {
                var output = _world.Get<OutputMessageComponent>(entity);
                output.Messages.Add(message);
            }
            // Also log to console for server admin
            Console.WriteLine($"[Combat] {message}");
        }

        // Helper to broadcast to everyone in the fight
        private void Broadcast(CombatTurnComponent combat, string message)
        {
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

            // Check for end of combat
            var livingCombatants = combat.TurnOrder.Where(e => _world.IsAlive(e)).ToList();
            if (livingCombatants.Count <= 1)
            {
                var winner = livingCombatants.FirstOrDefault();
                if (winner != Entity.Null)
                {
                    var winnerName = _world.Get<NameComponent>(winner).Name;
                    Broadcast(combat, $"\n--- COMBAT OVER! {winnerName} is victorious! ---");
                    _world.Remove<InCombatComponent>(winner);
                }
                else
                {
                    Broadcast(combat, "\n--- COMBAT OVER! All combatants were defeated. ---");
                }
                _world.Destroy(combatEntity);
                return;
            }

            var attackerEntity = combat.TurnOrder[combat.CurrentTurnIndex];

            if (!_world.IsAlive(attackerEntity))
            {
                AdvanceTurn(ref combat);
                return;
            }

            // Auto-queue attack for simple AI (anyone without a client connection)
            if (!_world.Has<OutputMessageComponent>(attackerEntity) && !_world.Has<AttackActionComponent>(attackerEntity))
            {
                var target = combat.TurnOrder.FirstOrDefault(e => e != attackerEntity && _world.IsAlive(e));
                if (target != Entity.Null)
                {
                    _world.Add(attackerEntity, new AttackActionComponent { Target = target });
                }
            }

            // If it's a player's turn and they haven't acted, wait.
            if (_world.Has<OutputMessageComponent>(attackerEntity) && !_world.Has<AttackActionComponent>(attackerEntity))
            {
                // Optional: Send a prompt "It is your turn!" once per round logic could go here
                return;
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

            if (!_world.IsAlive(target) || !_world.Has<InCombatComponent>(target))
            {
                SendMessage(attacker, "Target is invalid or has fled!");
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

            // 2. Compare to Armor Class
            if (totalAttack >= targetCombatStats.ArmorClass)
            {
                int damage = _diceRoller.Roll(6);
                ref var targetVitals = ref _world.Get<VitalsComponent>(target);
                targetVitals.CurrentHP -= damage;

                Broadcast(combat, $"{attackerName} hits {targetName} for {damage} damage!");

                if (targetVitals.CurrentHP <= 0)
                {
                    Broadcast(combat, $"{targetName} has been defeated!");

                    // Handle Player Death (Don't destroy, just remove from combat)
                    if (_world.Has<OutputMessageComponent>(target))
                    {
                        Broadcast(combat, $"{targetName} falls unconscious!");
                        _world.Remove<InCombatComponent>(target);
                    }
                    else
                    {
                        _world.Remove<InCombatComponent>(target);
                        _world.Destroy(target);
                    }
                }
            }
            else
            {
                Broadcast(combat, $"{attackerName} attacks {targetName} and misses.");
            }

            _world.Remove<AttackActionComponent>(attacker);
        }

        private void AdvanceTurn(ref CombatTurnComponent combat)
        {
            combat.CurrentTurnIndex++;
            if (combat.CurrentTurnIndex >= combat.TurnOrder.Count)
            {
                combat.CurrentTurnIndex = 0;
                Broadcast(combat, "--- Next Round ---");
            }
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}