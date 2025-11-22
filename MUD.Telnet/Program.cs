using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20;
using MUD.Server;
using MUD.Server.Data;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

var cts = new CancellationTokenSource();

// --- DATABASE SETUP ---
Console.WriteLine("--- Initializing Database ---");
var dbService = new DatabaseService();
dbService.InitializeDatabase();
// Ensure a test player account exists.
using (var db = new GameDbContext())
{
if (!db.Players.Any(p => p.AccountId == 12345))
{
db.Players.Add(new PlayerCharacter { AccountId = 12345, CharacterName = "Tester", Race = "human", Class = "fighter" });
db.SaveChanges();
}
}

// --- GAME WORLD SETUP ---
Console.WriteLine("--- Initializing Game World ---");
var world = World.Create();
var gameState = new GameState();
var ruleset = new D20Ruleset();

// Load all world content (NPCs, items, etc.)
ruleset.LoadContent(world, "Content/Aethelgard");

// Register all the systems that will run in the main loop.
var gameSystems = ruleset.RegisterSystems(world, gameState);
gameSystems.Initialize();
Console.WriteLine("Game world is ready.");

// Start the main game loop in the background.
_ = Task.Run(async () => {
    // Pass the CancellationToken to the loop.
    while (!cts.Token.IsCancellationRequested)
    {
        gameSystems.Update(new GameTime(1.0f));
        await Task.Delay(1000, cts.Token);
    }
    Console.WriteLine("Game loop has stopped.");
});

// --- TELNET SERVER SETUP ---
const int Port = 4000;
var listener = new TcpListener(IPAddress.Any, Port);

// --- GRACEFUL SHUTDOWN LOGIC ---
Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Shutdown signal received...");
    e.Cancel = true; // Prevent the app from terminating abruptly
    cts.Cancel();    // Signal the game loop and listener to stop
    listener.Stop(); // Stop the listener immediately
};

Console.WriteLine("--- MUD Telnet Server ---");
Console.WriteLine("Press Ctrl+C to shut down.");

try
{
    listener.Start();
    Console.WriteLine($"Server is listening on port {Port}...");

    // The loop will now exit gracefully when cts.Cancel() is called.
    while (!cts.Token.IsCancellationRequested)
    {
        // AcceptTcpClientAsync will throw an exception when the listener is stopped.
        TcpClient client = await listener.AcceptTcpClientAsync();
        var session = new TelnetSession(client, world, dbService);
        _ = Task.Run(session.HandleConnectionAsync);
    }
}
catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
{
    // This is the expected exception when listener.Stop() is called. We can ignore it.
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
finally
{
    Console.WriteLine("Telnet listener has stopped.");
}

Console.WriteLine("Server has shut down gracefully.");