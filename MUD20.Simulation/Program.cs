using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System;
using System.Collections.Generic;

// This is our dedicated test environment for running complex simulations.

// 1. Set up the world and ruleset.
var world = World.Create();
var gameState = new GameState();
var ruleset = new D20Ruleset();

// 2. Create the combatants manually for this test.
Console.WriteLine("--- Creating Test Combatants ---");
var player = world.Create(
    new NameComponent { Name = "Test Player" },
    // Fill in all the stats
    new CoreStatsComponent { Strength = 16, Dexterity = 14, Constitution = 14, Intelligence = 10, Wisdom = 12, Charisma = 8 },
    new CombatStatsComponent { ArmorClass = 16, BaseAttackBonus = 2 },
    new VitalsComponent { CurrentHP = 20, MaxHP = 20 }
);

var goblin = world.Create(
    new NameComponent { Name = "Test Goblin" },
    // Fill in all the stats
    new CoreStatsComponent { Strength = 11, Dexterity = 12, Constitution = 12, Intelligence = 8, Wisdom = 10, Charisma = 6 },
    new CombatStatsComponent { ArmorClass = 14, BaseAttackBonus = 1 },
    new VitalsComponent { CurrentHP = 8, MaxHP = 8 }
); 

Console.WriteLine("Combatants created.");

// 3. Create the request to start combat.
Console.WriteLine("\n--- Simulating Combat Start! ---\n");
world.Create(new StartCombatRequestComponent
{
    Combatants = new List<Entity> { player, goblin }
});

// 4. Register all the game systems.
var gameSystems = ruleset.RegisterSystems(world, gameState);
gameSystems.Initialize();

// 5. Run the simulation loop.
Console.WriteLine("--- Starting Combat Simulation Loop ---");
int round = 1;
// We'll run for a max of 20 turns to prevent an infinite loop.
while (round <= 20)
{
    Console.WriteLine($"\n--- Round {round} ---");
    gameSystems.Update(new GameTime(1.0f)); // Pass a time delta of 1.0

    // Check if combat is over (no more CombatTurnComponent exists).
    var combatQuery = new QueryDescription().WithAll<CombatTurnComponent>();
    if (world.CountEntities(in combatQuery) == 0)
    {
        Console.WriteLine("\n--- Combat has ended! ---");
        break;
    }

    round++;
    Thread.Sleep(500); // Pause to make the log readable.
}