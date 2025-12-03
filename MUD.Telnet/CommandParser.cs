using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using MUD.Rulesets.D20.Components;
using MUD.Telnet.Commands;

public class CommandParser
{
    private readonly TelnetSession _session;
    private readonly World _world;

    // A dictionary to hold all our command objects.
    private readonly Dictionary<string, ICommand> _commands;

    public CommandParser(TelnetSession session, World world)
    {
        _session = session;
        _world = world;

        // Initialize our commands.
        _commands = new Dictionary<string, ICommand>
        {
            { "attack", new Attack() },
            { "a", new Attack() },
            { "move", new MoveCommand() },
            { "m", new MoveCommand() },
            { "look", new LookCommand() },
            { "l", new LookCommand() },
            { "inventory", new InventoryCommand() },
            { "i", new InventoryCommand() },
            { "get", new GetCommand() },
            { "take", new GetCommand() },
            { "equip", new EquipCommand() },
            { "wield", new EquipCommand() },
            { "wake", new WakeCommand() }
        };
    }

    public async Task ParseCommand(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText)) return;

        var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0];
        var args = parts.Skip(1).ToArray();

        // --- NEW: Status Check ---
        if (_session.PlayerEntity.HasValue)
        {
            var player = _session.PlayerEntity.Value;

            // FIX: Only block commands if the player actually has the UnconsciousComponent
            if (_world.Has<UnconsciousComponent>(player))
            {
                if (commandName.ToLower() != "quit" && commandName.ToLower() != "wake")
                {
                    await _session.WriteLineAsync("You are unconscious and cannot act... (Type 'wake' for options)");
                    return;
                }
            }
        }
        // -------------------------

        if (_commands.TryGetValue(commandName, out var command))
        {
            await command.ExecuteAsync(_session, _world, args);
        }
        else
        {
            await _session.WriteLineAsync($"Unknown command: {commandName}");
        }
    }
}