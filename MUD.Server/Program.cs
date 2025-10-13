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

// Step 1: Initialize the database service.
var dbService = new DatabaseService();
dbService.InitializeDatabase();

// Ensure our test player exists in the database.
using (var db = new GameDbContext())
{
    if (!db.Players.Any(p => p.AccountId == 12345))
    {
        db.Players.Add(new PlayerCharacter
        {
            AccountId = 12345,
            CharacterName = "Tester",
            Race = "human",
            Class = "fighter"
        });
        db.SaveChanges();
    }
}

// Step 2: Create the foundational ECS World and GameState.
World ecsWorld = World.Create();
GameState gameState = new GameState();
Console.WriteLine("ECS World and GameState created.");

// Step 3: Load the ruleset.
IRuleset ruleset = new D20Ruleset();
Console.WriteLine($"Ruleset '{ruleset.Name}' has been loaded.");

// Step 4: Create a login request entity.
Console.WriteLine("Simulating a player login request for Account ID 12345...");
ecsWorld.Create(new PlayerLoginRequestComponent { AccountId = 12345 });

// Step 5: Manually run the creation system to process the login request.
var creationSystem = new CharacterCreationSystem(ecsWorld, dbService);
Entity? playerEntity = creationSystem.Update(new GameTime(0)); // This will now work correctly.

// Step 6: Load all the other world content (NPCs, items, etc.) from the manifest.
Console.WriteLine("Loading world content from manifest...");
ruleset.LoadContent(ecsWorld, "Content/Aethelgard");

// Step 7: Register all the systems that will run in the main game loop.
Group<GameTime> gameSystems = ruleset.RegisterSystems(ecsWorld, gameState);
gameSystems.Initialize();
Console.WriteLine("Ruleset initialized successfully.");

// Step 8: Start the main game loop.
Console.WriteLine("Starting main game loop...");
bool firstTick = true; // A flag to ensure our test only runs once.

while (true)
{
    // On the very first tick of the loop, create the skill check request.
    if (firstTick && playerEntity.HasValue)
    {
        Console.WriteLine("\n--- Simulating a Perception skill check for the player... ---\n");
        ecsWorld.Create(new SkillCheckRequestComponent
        {
            Performer = playerEntity.Value,
            Skill = Skill.Perception,
            DifficultyClass = 15
        });
        firstTick = false; // Set the flag so this doesn't run again.
    }

    // Now, the SkillCheckSystem will find and process the request.
    gameSystems.Update(new GameTime(0.1f));
    Thread.Sleep(500); // Slowed down to make the output easy to read
}