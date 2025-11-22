using Arch.Core;
using MUD.Rulesets.D20.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InventoryCommand : ICommand
{
    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;

        await session.WriteLineAsync("You are carrying:");
        var inventory = world.Get<InventoryComponent>(session.PlayerEntity.Value);

        if (inventory.Items.Count == 0)
        {
            await session.WriteLineAsync("  Nothing.");
        }
        else
        {
            var itemNames = new List<string>();
            foreach (var itemEntity in inventory.Items)
            {
                itemNames.Add(world.Get<NameComponent>(itemEntity).Name);
            }
            foreach (var itemName in itemNames)
            {
                await session.WriteLineAsync($"  - {itemName}");
            }
        }
    }
}