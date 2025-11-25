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

            // FIX 1: Rename list to 'requestsHandled' to be clear we aren't destroying entities
            var requestsHandled = new List<Entity>();

            _world.Query(in requestQuery, (Entity entity, ref StartCombatRequestComponent request) =>
            {
                // Check if combat is already running. If so, ignore new requests for now.
                var combatStateQuery = new QueryDescription().WithAll<CombatTurnComponent>();
                if (_world.CountEntities(in combatStateQuery) > 0)
                {
                    requestsHandled.Add(entity);
                    return;
                }

                Console.WriteLine("\n--- COMBAT BEGINS! ---");

                // Roll initiative for each combatant.
                foreach (var combatantEntity in request.Combatants)
                {
                    if (!_world.IsAlive(combatantEntity)) continue;

                    // Default dex to 10 if missing
                    int dex = 10;
                    if (_world.Has<CoreStatsComponent>(combatantEntity))
                    {
                        dex = _world.Get<CoreStatsComponent>(combatantEntity).Dexterity;
                    }

                    int dexModifier = (dex - 10) / 2;
                    int initiativeRoll = _random.Next(1, 21) + dexModifier;

                    // Add the InCombatComponent and set their initiative.
                    // Note: Arch handles adding components to existing entities gracefully.
                    if (!_world.Has<InCombatComponent>(combatantEntity))
                    {
                        _world.Add(combatantEntity, new InCombatComponent { Initiative = initiativeRoll });
                    }
                    else
                    {
                        var ic = _world.Get<InCombatComponent>(combatantEntity);
                        ic.Initiative = initiativeRoll;
                        _world.Set(combatantEntity, ic);
                    }

                    // Safety check for Name
                    string name = "Unknown";
                    if (_world.Has<NameComponent>(combatantEntity))
                        name = _world.Get<NameComponent>(combatantEntity).Name;

                    Console.WriteLine($"  {name} rolls initiative: {initiativeRoll}");
                    // NOTIFY PLAYERS
                    SendMessage(combatantEntity, $"You roll initiative: {initiativeRoll}");
                }

                // Sort the combatants by initiative, highest first.
                // Ensure we only sort living, valid combatants
                var turnOrder = request.Combatants
                    .Where(c => _world.IsAlive(c) && _world.Has<InCombatComponent>(c))
                    .OrderByDescending(c => _world.Get<InCombatComponent>(c).Initiative)
                    .ToList();

                // Create the singleton entity to manage the combat turn.
                _world.Create(new CombatTurnComponent
                {
                    TurnOrder = turnOrder,
                    CurrentTurnIndex = 0,
                    RoundNumber = 1
                });

                Console.WriteLine("----------------------\n");
                // Notify everyone that combat has started
                foreach (var p in request.Combatants)
                {
                    if (_world.IsAlive(p)) SendMessage(p, "Combat has begun!");
                }

                // Mark this request as handled
                requestsHandled.Add(entity);
            });

            // FIX 2: REMOVE the component, DO NOT DESTROY the entity
            foreach (var entity in requestsHandled)
            {
                _world.Remove<StartCombatRequestComponent>(entity);
            }
        }
        private void SendMessage(Entity entity, string message)
        {
            if (_world.Has<OutputMessageComponent>(entity))
            {
                _world.Get<OutputMessageComponent>(entity).Messages.Add(message);
            }
            Console.WriteLine(message);
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}