using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;

namespace MUD.Tests
{
    [TestClass]
    public class SkillCheckSystemTests
    {
        private World _world;
        private GameState _gameState;
        private MockDiceRoller _mockDiceRoller;
        private SkillCheckSystem _skillCheckSystem;
        private Entity _player;

        [TestInitialize]
        public void TestSetup()
        {
            _world = World.Create();
            _gameState = new GameState();
            _mockDiceRoller = new MockDiceRoller();
            _skillCheckSystem = new SkillCheckSystem(_world, _gameState, _mockDiceRoller);

            _player = _world.Create(
                new NameComponent { Name = "Test Player" },
                new CoreStatsComponent { Dexterity = 14 }, // +2 modifier
                new SkillsComponent { Stealth = 5 }      // +5 skill ranks
            );
        }

        [TestMethod]
        public void SkillCheck_Succeeds_WhenRollIsSufficient()
        {
            // Total modifier = +7. DC is 15. A roll of 8+ succeeds.
            _mockDiceRoller.NextRoll = 10; // Force a success
            _world.Create(new SkillCheckRequestComponent { Performer = _player, Skill = Skill.Stealth, DifficultyClass = 15 });

            _skillCheckSystem.Update(new GameTime(0));

            var query = new QueryDescription().WithAll<SkillCheckRequestComponent>();
            Assert.AreEqual(0, _world.CountEntities(in query));
        }

        [TestMethod]
        public void SkillCheck_Fails_WhenRollIsInsufficient()
        {
            // Total modifier = +7. DC is 15. A roll of 7 or less fails.
            _mockDiceRoller.NextRoll = 5; // Force a failure
            _world.Create(new SkillCheckRequestComponent { Performer = _player, Skill = Skill.Stealth, DifficultyClass = 15 });

            _skillCheckSystem.Update(new GameTime(0));

            var query = new QueryDescription().WithAll<SkillCheckRequestComponent>();
            Assert.AreEqual(0, _world.CountEntities(in query));
        }
    }
}