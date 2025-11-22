using Arch.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            { "look", new LookCommand() },
            { "l", new LookCommand() },
            { "inventory", new InventoryCommand() },
            { "i", new InventoryCommand() },
            // --- ADD THESE TWO LINES ---
            { "get", new GetCommand() },
            { "take", new GetCommand() },
            { "equip", new EquipCommand() },
            { "wield", new EquipCommand() }
        };
    }

    public async Task ParseCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        string[] parts = input.Trim().Split(' ');
        string commandWord = parts[0].ToLower();
        string[] args = parts.Skip(1).ToArray();

        // Find the command in our dictionary.
        if (_commands.TryGetValue(commandWord, out ICommand? command))
        {
            // If found, execute it.
            await command.ExecuteAsync(_session, _world, args);
        }
        else
        {
            await _session.WriteLineAsync($"Unknown command: {commandWord}");
        }
    }
}