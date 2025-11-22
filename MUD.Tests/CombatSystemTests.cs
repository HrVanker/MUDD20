using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System.Collections.Generic;
using System.Linq;

namespace MUD.Tests
{
    [TestClass]
    public class CombatSystemTests
    {
        // --- THE FIX: Declare all shared variables here at the class level ---
        private World _world;
        private GameState _gameState;
        private CombatSystem _combatSystem;
        private Entity _player;
        private Entity _goblin;
        private MockDiceRoller _mockDiceRoller; // This makes it accessible everywhere in the class

        [TestInitialize]
        public void TestSetup()
        {
            _world = World.Create();
            _gameState = new GameState();
            _mockDiceRoller = new MockDiceRoller(); // Initialize the mock here
            _combatSystem = new CombatSystem(_world, _gameState, _mockDiceRoller); // Inject it

            _player = _world.Create(
                new NameComponent { Name = "Test Player" },
                new CombatStatsComponent { ArmorClass = 16, BaseAttackBonus = 5 },
                new VitalsComponent { CurrentHP = 20, MaxHP = 20 }
            );
            _world.Add(_player, new InCombatComponent());

            _goblin = _world.Create(
                new NameComponent { Name = "Test Goblin" },
                new CombatStatsComponent { ArmorClass = 14, BaseAttackBonus = 1 },
                new VitalsComponent { CurrentHP = 8, MaxHP = 8 }
            );
            _world.Add(_goblin, new InCombatComponent());
        }

        [TestMethod]
        public void CombatSystem_AttackHits_WhenRollIsSufficient()
        {
            // --- ARRANGE ---
            // A roll of 15 guarantees a hit (15 + BAB 5 = 20 vs AC 14)
            // The damage roll will also be 15, which is enough to defeat the goblin.
            _mockDiceRoller.NextRoll = 15;

            _world.Create(new CombatTurnComponent
            {
                TurnOrder = new List<Entity> { _player, _goblin },
                CurrentTurnIndex = 0
            });
            _world.Add(_player, new AttackActionComponent { Target = _goblin });

            // --- ACT ---
            _combatSystem.Update(new GameTime(0));

            // --- ASSERT ---
            // THE FIX: Instead of checking the goblin's HP, we assert that the
            // goblin entity has been destroyed, which is the correct outcome of the fatal hit.
            Assert.IsFalse(_world.IsAlive(_goblin), "The goblin should have been defeated and destroyed.");
        }

        [TestMethod]
        public void CombatSystem_AttackMisses_WhenRollIsInsufficient()
        {
            // --- ARRANGE ---
            // We can't guarantee a miss with random rolls, so we'll check the opposite case:
            // Give the goblin an impossibly high AC. The attack should never hit.
            _world.Set(_goblin, new CombatStatsComponent { ArmorClass = 99, BaseAttackBonus = 1 });

            _world.Create(new CombatTurnComponent
            {
                TurnOrder = new List<Entity> { _player, _goblin },
                CurrentTurnIndex = 0
            });
            _world.Add(_player, new AttackActionComponent { Target = _goblin });

            // --- ACT ---
            _combatSystem.Update(new GameTime(0));

            // --- ASSERT ---
            // Verify that the goblin's HP is unchanged.
            var goblinVitals = _world.Get<VitalsComponent>(_goblin);
            Assert.AreEqual(goblinVitals.MaxHP, goblinVitals.CurrentHP, "Goblin should not have taken damage.");
        }

        [TestMethod]
        public void CombatSystem_EntityDies_WhenHpReachesZero()
        {
            // --- ARRANGE ---
            // Set the goblin's HP to 1 so any successful hit will defeat it.
            _world.Set(_goblin, new VitalsComponent { CurrentHP = 1, MaxHP = 8 });

            _world.Create(new CombatTurnComponent
            {
                TurnOrder = new List<Entity> { _player, _goblin },
                CurrentTurnIndex = 0
            });
            _world.Add(_player, new AttackActionComponent { Target = _goblin });

            // --- ACT ---
            // We run the update loop multiple times to ensure a hit occurs.
            for (int i = 0; i < 20; i++)
            {
                if (!_world.IsAlive(_goblin)) break;
                _combatSystem.Update(new GameTime(0));
            }

            // --- ASSERT ---
            // Verify that the goblin entity has been destroyed.
            Assert.IsFalse(_world.IsAlive(_goblin), "Goblin entity should have been destroyed.");
        }
    }
}