using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using MUD.Server;
using MUD.Server.Data;
using System.Linq;

namespace MUD.Tests
{
    [TestClass]
    public class CharacterCreationSystemTests
    {
        [TestMethod]
        public void CharacterCreation_CreatesPlayerEntity_FromDatabaseAndTemplates()
        {
            // --- ARRANGE ---
            var world = World.Create();
            var dbService = new DatabaseService();
            dbService.InitializeDatabase(); // Ensure db exists
            using (var db = new GameDbContext())
            {
                if (!db.Players.Any(p => p.AccountId == 999))
                {
                    db.Players.Add(new PlayerCharacter { AccountId = 999, CharacterName = "CreationTest", Race = "human", Class = "fighter" });
                    db.SaveChanges();
                }
            }

            world.Create(new PlayerLoginRequestComponent { AccountId = 999 });
            var creationSystem = new CharacterCreationSystem(world, dbService);

            // --- ACT ---
            var createdEntity = creationSystem.Update(new GameTime(0));

            // --- ASSERT ---
            Assert.IsTrue(createdEntity.HasValue, "System should have created an entity.");
            Assert.IsTrue(world.IsAlive(createdEntity.Value), "Created entity should be alive.");

            var name = world.Get<NameComponent>(createdEntity.Value);
            Assert.AreEqual("CreationTest", name.Name);

            var stats = world.Get<CoreStatsComponent>(createdEntity.Value);
            // Base human fighter (18 Str) + Human racial (+1) = 19
            Assert.AreEqual(19, stats.Strength);
        }
    }
}