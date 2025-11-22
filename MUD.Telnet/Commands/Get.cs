using Arch.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Threading.Tasks;

public class GetCommand : ICommand
{
    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;

        if (args.Length == 0)
        {
            await session.WriteLineAsync("Get what?");
            return;
        }

        string targetName = args[0];
        Entity targetEntity = Entity.Null;
        var playerLocation = world.Get<LocationComponent>(session.PlayerEntity.Value);

        // Find an item in the same room with a matching name.
        var itemQuery = new QueryDescription().WithAll<ItemComponent, NameComponent, LocationComponent>();
        world.Query(in itemQuery, (Entity entity, ref NameComponent name, ref LocationComponent loc) => {
            if (loc.RoomId == playerLocation.RoomId && name.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetEntity = entity;
            }
        });

        if (targetEntity != Entity.Null)
        {
            // Get the player's inventory, add the item, and then update it in the world.
            var playerInventory = world.Get<InventoryComponent>(session.PlayerEntity.Value);
            playerInventory.Items.Add(targetEntity);
            world.Set(session.PlayerEntity.Value, playerInventory);

            // Remove the item from the room.
            world.Remove<LocationComponent>(targetEntity);
            await session.WriteLineAsync($"You take the {targetName}.");
        }
        else
        {
            await session.WriteLineAsync("You don't see that here.");
        }
    }
}