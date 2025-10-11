using System;
using System.Threading;
using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20;
using MUD.Rulesets.D20.Components;
using Tomlyn;

Console.WriteLine("Server is starting up...");

World ecsWorld = World.Create();
GameState gameState = new GameState();
Console.WriteLine("ECS World and GameState created.");

IRuleset ruleset = new D20Ruleset();
Console.WriteLine($"Ruleset '{ruleset.Name}' has been loaded.");

ruleset.LoadContent(ecsWorld, "Content");

// Use the correct Group<GameTime> type
Group<GameTime> gameSystems = ruleset.RegisterSystems(ecsWorld, gameState);
gameSystems.Initialize();
Console.WriteLine("Ruleset initialized successfully.");

//Console.WriteLine("Creating a manual Test Dummy entity...");
//ecsWorld.Create(
//    new NameComponent { Name = "Test Dummy" },
//    new CoreStatsComponent { Strength = 99, Dexterity = 99 }
//);

Console.WriteLine("Starting main game loop...");
while (true)
{
    gameSystems.Update(new GameTime(0.1f));
    Thread.Sleep(100);
}