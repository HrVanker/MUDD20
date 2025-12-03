using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class TelnetSession
{
    private readonly TcpClient _client;
    private readonly World _world;
    private readonly IDatabaseService _dbService;
    public IDatabaseService DbService => _dbService;
    private readonly CommandParser _parser;
    private StreamWriter? _writer;
    public ulong AccountId { get; private set; }

    public Entity? PlayerEntity { get; private set; }

    private readonly EntityFactory _entityFactory; // Store it

    public TelnetSession(TcpClient client, World world, IDatabaseService dbService, EntityFactory factory)
    {
        _client = client;
        _world = world;
        _dbService = dbService;
        _entityFactory = factory; // <--- Store it
        _parser = new CommandParser(this, _world, _entityFactory); // Pass to parser
    }

    public async Task HandleConnectionAsync()
    {
        try
        {
            Console.WriteLine($"Session started for {_client.Client.RemoteEndPoint}");
            await Task.Delay(50); // Allow connection to stabilize

            using (var stream = _client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            {
                _writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

                // --- LOGIN SEQUENCE ---
                try
                {
                    await WriteLineAsync("Welcome to the MUD!");
                    await WriteLineAsync("Please enter your Account ID to log in (e.g., 12345):");
                }
                catch (IOException) { return; }

                string? accountIdInput = await reader.ReadLineAsync();

                if (ulong.TryParse(accountIdInput, out ulong accountId))
                {
                    AccountId = accountId;
                    _world.Create(new PlayerLoginRequestComponent { AccountId = accountId });
                    var creationSystem = new CharacterCreationSystem(_world, _dbService);
                    PlayerEntity = creationSystem.Update(new GameTime(0));
                }

                if (!PlayerEntity.HasValue)
                {
                    try { await WriteLineAsync("Login failed. Disconnecting."); } catch { }
                    return;
                }

                // --- NEW: Initialize the Output Mailbox ---
                _world.Add(PlayerEntity.Value, new OutputMessageComponent { Messages = new List<string>() });

                // --- NEW: Start the Background Output Loop ---
                // This runs parallel to the Command Loop to push messages to the client
                _ = Task.Run(ProcessOutputQueue);

                var playerName = _world.Get<NameComponent>(PlayerEntity.Value).Name;
                await WriteLineAsync($"Welcome, {playerName}!");

                // --- COMMAND LOOP ---
                while (_client.Connected)
                {
                    try
                    {
                        await _writer.WriteAsync("> ");
                        string? command = await reader.ReadLineAsync();

                        if (command == null || command.Trim().ToLower() == "exit") break;
                        await _parser.ParseCommand(command);
                    }
                    catch (IOException) { break; }
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
            // Cleanup: If the player is still in the world, remove their output component or Entity
            if (PlayerEntity.HasValue && _world.IsAlive(PlayerEntity.Value))
            {
                _world.Remove<OutputMessageComponent>(PlayerEntity.Value);
            }
            _client.Close();
        }
    }

    private async Task ProcessOutputQueue()
    {
        while (_client.Connected)
        {
            if (PlayerEntity.HasValue && _world.IsAlive(PlayerEntity.Value))
            {
                // Check if we have messages pending
                if (_world.Has<OutputMessageComponent>(PlayerEntity.Value))
                {
                    var output = _world.Get<OutputMessageComponent>(PlayerEntity.Value);
                    if (output.Messages != null && output.Messages.Count > 0)
                    {
                        // Send all messages
                        foreach (var msg in output.Messages)
                        {
                            await WriteLineAsync(msg);
                        }
                        // Clear the queue
                        output.Messages.Clear();
                    }
                }
            }
            // Poll every 100ms
            await Task.Delay(100);
        }
    }

    public async Task WriteLineAsync(string message)
    {
        if (_writer != null && _client.Connected)
        {
            await _writer.WriteLineAsync(message);
        }
    }
}