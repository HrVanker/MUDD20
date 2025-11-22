using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using MUD.Server;
using MUD.Server.Data;
using System.Linq;
using System;
using System.Threading;

Console.WriteLine("Server is starting up...");

// Step 1: Initialize the database.
var dbService = new DatabaseService();
dbService.InitializeDatabase();
using (var db = new GameDbContext())
{
    if (!db.Players.Any(p => p.AccountId == 12345))
    {
        db.Players.Add(new PlayerCharacter { AccountId = 12345, CharacterName = "Tester", Race = "human", Class = "fighter" });
        db.SaveChanges();
    }
}

// Step 2: Create the ECS World.
World ecsWorld = World.Create();
GameState gameState = new GameState();
Console.WriteLine("ECS World and GameState created.");

// Step 3: Load the ruleset.
IRuleset ruleset = new D20Ruleset();
Console.WriteLine($"Ruleset '{ruleset.Name}' has been loaded.");

// Step 4: Create a login request and run the system to process it.
Console.WriteLine("Simulating a player login request for Account ID 12345...");
ecsWorld.Create(new PlayerLoginRequestComponent { AccountId = 12345 });
var creationSystem = new CharacterCreationSystem(ecsWorld, dbService);
creationSystem.Update(new GameTime(0));

// Step 5: Load all other world content like NPCs.
Console.WriteLine("Loading world content from manifest...");
ruleset.LoadContent(ecsWorld, "Content/Aethelgard");

// Step 6: Register the systems that run every tick.
Group<GameTime> gameSystems = ruleset.RegisterSystems(ecsWorld, gameState);
gameSystems.Initialize();
Console.WriteLine("Ruleset initialized successfully.");

// Step 7: Start the main game loop.
Console.WriteLine("Starting main game loop...");
while (true)
{
    // The loop is now clean and only runs the game systems.
    gameSystems.Update(new GameTime(0.1f));
    Thread.Sleep(100);
}