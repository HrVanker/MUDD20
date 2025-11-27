using System;
using System.Threading.Tasks;
using Arch.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;

public class GetCommand : ICommand
{
    public string Name => "get";

    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;
        var player = session.PlayerEntity.Value;

        if (args.Length == 0)
        {
            await session.WriteLineAsync("Get what?");
            return;
        }

        string targetName = args[0];
        Entity targetEntity = Entity.Null;
        var playerLoc = world.Get<LocationComponent>(player);

        // 1. Find Item
        var itemQuery = new QueryDescription().WithAll<ItemComponent, NameComponent, LocationComponent>();
        world.Query(in itemQuery, (Entity entity, ref NameComponent name, ref LocationComponent loc) => {
            if (loc.RoomId == playerLoc.RoomId && name.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetEntity = entity;
            }
        });

        if (targetEntity != Entity.Null)
        {
            // 2. Check Distance
            var targetLoc = world.Get<LocationComponent>(targetEntity);
            int distance = Math.Max(Math.Abs(playerLoc.X - targetLoc.X), Math.Abs(playerLoc.Y - targetLoc.Y));

            // Allow pickup if 0 (same square) or 1 (adjacent)
            if (distance > 1)
            {
                await session.WriteLineAsync($"The {targetName} is too far. Moving towards it...");
                if (world.Has<MoveToRequestComponent>(player)) world.Remove<MoveToRequestComponent>(player);

                world.Add(player, new MoveToRequestComponent { TargetX = targetLoc.X, TargetY = targetLoc.Y });
                return;
            }

            // 3. Pick Up
            var playerInventory = world.Get<InventoryComponent>(player);
            playerInventory.Items.Add(targetEntity);
            world.Set(player, playerInventory);

            world.Remove<LocationComponent>(targetEntity);
            await session.WriteLineAsync($"You take the {targetName}.");
        }
        else
        {
            await session.WriteLineAsync("You don't see that here.");
        }
    }
}