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
Entity? playerEntity = creationSystem.Update(new GameTime(0)); // This captures the player entity.

// Step 5: Load all other world content like NPCs.
Console.WriteLine("Loading world content from manifest...");
ruleset.LoadContent(ecsWorld, "Content/Aethelgard");

// Step 6: Register the systems that run every tick.
Group<GameTime> gameSystems = ruleset.RegisterSystems(ecsWorld, gameState);
gameSystems.Initialize();
Console.WriteLine("Ruleset initialized successfully.");

// Step 7: Start the main game loop.
Console.WriteLine("Starting main game loop...");
bool firstTick = true; // A flag to ensure our test runs only once.

while (true)
{
    // THE FIX: The skill check is created INSIDE the loop.
    if (firstTick && playerEntity.HasValue)
    {
        Console.WriteLine("\n--- Simulating a Perception skill check for the player... ---\n");
        ecsWorld.Create(new SkillCheckRequestComponent
        {
            Performer = playerEntity.Value,
            Skill = Skill.Perception,
            DifficultyClass = 15
        });
        firstTick = false; // Prevent this from running on every tick.
    }

    // The game systems (including SkillCheckSystem) are updated here.
    gameSystems.Update(new GameTime(0.1f));
    Thread.Sleep(500);
}