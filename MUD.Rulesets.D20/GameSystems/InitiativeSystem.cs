using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MUD.Rulesets.D20.GameSystems
{
    public class InitiativeSystem : ISystem<GameTime>
    {
        private readonly World _world;
        private readonly Random _random = new Random();

        public InitiativeSystem(World world, GameState gameState)
        {
            _world = world;
        }

        public void Update(in GameTime gameTime)
        {
            // Find any requests to start combat.
            var requestQuery = new QueryDescription().WithAll<StartCombatRequestComponent>();
            var entitiesToDestroy = new List<Entity>();

            _world.Query(in requestQuery, (Entity entity, ref StartCombatRequestComponent request) =>
            {
                // Check if combat is already running. If so, ignore new requests for now.
                var combatStateQuery = new QueryDescription().WithAll<CombatTurnComponent>();
                if (_world.CountEntities(in combatStateQuery) > 0)
                {
                    entitiesToDestroy.Add(entity);
                    return;
                }

                Console.WriteLine("\n--- COMBAT BEGINS! ---");

                // Roll initiative for each combatant.
                foreach (var combatantEntity in request.Combatants)
                {
                    if (!_world.IsAlive(combatantEntity)) continue;

                    var stats = _world.Get<CoreStatsComponent>(combatantEntity);
                    int dexModifier = (stats.Dexterity - 10) / 2;
                    int initiativeRoll = _random.Next(1, 21) + dexModifier;

                    // Add the InCombatComponent and set their initiative.
                    _world.Add(combatantEntity, new InCombatComponent { Initiative = initiativeRoll });

                    var name = _world.Get<NameComponent>(combatantEntity).Name;
                    Console.WriteLine($"  {name} rolls initiative: {initiativeRoll}");
                }

                // Sort the combatants by initiative, highest first.
                var turnOrder = request.Combatants
                    .OrderByDescending(c => _world.Get<InCombatComponent>(c).Initiative)
                    .ToList();

                // Create the singleton entity to manage the combat turn.
                _world.Create(new CombatTurnComponent
                {
                    TurnOrder = turnOrder,
                    CurrentTurnIndex = 0
                });

                Console.WriteLine("----------------------\n");
                entitiesToDestroy.Add(entity);
            });

            foreach (var entity in entitiesToDestroy) { _world.Destroy(entity); }
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}