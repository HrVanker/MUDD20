using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class TelnetSession
{
    private readonly TcpClient _client;
    private readonly World _world;
    private readonly IDatabaseService _dbService;
    private readonly CommandParser _parser;
    private StreamWriter? _writer;

    // This will hold the entity that represents this specific player.
    public Entity? PlayerEntity { get; private set; }

    public TelnetSession(TcpClient client, World world, IDatabaseService dbService)
    {
        _client = client;
        _world = world;
        _dbService = dbService;
        // The parser now gets a reference to this session.
        _parser = new CommandParser(this, _world);
    }

    public async Task HandleConnectionAsync()
    {
        try
        {
            Console.WriteLine($"Session started for {_client.Client.RemoteEndPoint}");
            using (var stream = _client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            {
                _writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

                // --- LOGIN SEQUENCE ---
                await WriteLineAsync("Welcome to the MUD!");
                await WriteLineAsync("Please enter your Account ID to log in (e.g., 12345):");
                string? accountIdInput = await reader.ReadLineAsync();

                if (ulong.TryParse(accountIdInput, out ulong accountId))
                {
                    // Create a login request and run the creation system.
                    _world.Create(new PlayerLoginRequestComponent { AccountId = accountId });
                    var creationSystem = new CharacterCreationSystem(_world, _dbService);
                    PlayerEntity = creationSystem.Update(new GameTime(0));
                }

                if (!PlayerEntity.HasValue)
                {
                    await WriteLineAsync("Login failed. Disconnecting.");
                    return; // End the session if login fails.
                }

                var playerName = _world.Get<NameComponent>(PlayerEntity.Value).Name;
                await WriteLineAsync($"Welcome, {playerName}!");
                // --- END LOGIN SEQUENCE ---


                // --- COMMAND LOOP ---
                while (_client.Connected)
                {
                    await _writer.WriteAsync("> ");
                    string? command = await reader.ReadLineAsync();

                    if (command == null || command.Trim().ToLower() == "exit")
                    {
                        break;
                    }
                    await _parser.ParseCommand(command);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in session: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Session ended for {_client.Client.RemoteEndPoint}");
            _client.Close();
        }
    }

    // Helper method to send a line of text to the player.
    public async Task WriteLineAsync(string message)
    {
        if (_writer != null)
        {
            await _writer.WriteLineAsync(message);
        }
    }
}