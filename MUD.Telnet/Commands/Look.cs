using Arch.Core;
using MUD.Rulesets.D20.Components;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class LookCommand : ICommand
{
    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;

        var playerLocation = world.Get<LocationComponent>(session.PlayerEntity.Value);

        // 1. Find the Room Entity to get Title/Description
        string roomTitle = "The Void";
        string roomDesc = "You are floating in formless nothingness.";
        int width = 0;
        int height = 0;

        var roomQuery = new QueryDescription().WithAll<RoomComponent>();
        world.Query(in roomQuery, (ref RoomComponent room) =>
        {
            if (room.AreaId == playerLocation.RoomId)
            {
                roomTitle = $"{room.Title} (ID: {room.AreaId})";
                roomDesc = room.Description;
                width = room.Width;
                height = room.Height;
            }
        });

        // 2. Build the Output
        var sb = new StringBuilder();
        sb.AppendLine($"\n=== {roomTitle} ===");
        sb.AppendLine(roomDesc);
        sb.AppendLine($"[Map Size: {width}x{height}] [Your Location: {playerLocation.X},{playerLocation.Y}]");
        sb.AppendLine("---------------------------------");

        // 3. Find Items/NPCs
        var entityQuery = new QueryDescription().WithAll<NameComponent, LocationComponent>();
        var visibleEntities = new List<string>();

        world.Query(in entityQuery, (Entity entity, ref NameComponent name, ref LocationComponent loc) =>
        {
            // Don't list yourself or the room itself (rooms have LocationComponent too!)
            if (entity != session.PlayerEntity.Value &&
                loc.RoomId == playerLocation.RoomId &&
                !world.Has<RoomComponent>(entity)) // Don't show the room entity in the list
            {
                visibleEntities.Add($"{name.Name} [at {loc.X},{loc.Y}]");
            }
        });

        if (visibleEntities.Count > 0)
        {
            sb.AppendLine("You see:");
            foreach (var name in visibleEntities)
            {
                sb.AppendLine($" - {name}");
            }
        }
        else
        {
            sb.AppendLine("You see no one else here.");
        }

        await session.WriteLineAsync(sb.ToString());
    }
}