// In MUD.Tests/InitiativeSystemTests.cs

using Microsoft.VisualStudio.TestTools.UnitTesting; // <-- This is the missing line
using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System.Collections.Generic;

namespace MUD.Tests
{
    [TestClass]
    public class InitiativeSystemTests
    {
        [TestMethod]
        public void InitiativeSystem_RollsInitiative_And_CreatesCombatState()
        {
            // --- ARRANGE ---
            var world = World.Create();
            var gameState = new GameState();

            var player = world.Create(
                new NameComponent { Name = "Test Player" },
                new CoreStatsComponent { Dexterity = 14 }
            );
            var goblin = world.Create(
                new NameComponent { Name = "Test Goblin" },
                new CoreStatsComponent { Dexterity = 12 }
            );

            world.Create(new StartCombatRequestComponent
            {
                Combatants = new List<Entity> { player, goblin }
            });

            var initiativeSystem = new InitiativeSystem(world, gameState);

            // --- ACT ---
            initiativeSystem.Update(new GameTime(0));

            // --- ASSERT ---
            var combatQuery = new QueryDescription().WithAll<CombatTurnComponent>();
            int combatCount = world.CountEntities(in combatQuery);
            Assert.AreEqual(1, combatCount, "A CombatTurnComponent should have been created.");

            Assert.IsTrue(world.Has<InCombatComponent>(player), "Player should be in combat.");
            Assert.IsTrue(world.Has<InCombatComponent>(goblin), "Goblin should be in combat.");

            world.Query(in combatQuery, (ref CombatTurnComponent combat) =>
            {
                Assert.AreEqual(2, combat.TurnOrder.Count, "Turn order should have two combatants.");
            });
        }
    }
}