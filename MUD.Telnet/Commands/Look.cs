using Arch.Core;
using MUD.Rulesets.D20.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LookCommand : ICommand
{
    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;

        await session.WriteLineAsync("You look around and see:");
        var namesInRoom = new List<string>();
        var query = new QueryDescription().WithAll<NameComponent, LocationComponent>();
        var playerLocation = world.Get<LocationComponent>(session.PlayerEntity.Value);

        world.Query(in query, (Entity entity, ref NameComponent name, ref LocationComponent loc) =>
        {
            if (entity != session.PlayerEntity.Value && loc.RoomId == playerLocation.RoomId)
            {
                namesInRoom.Add(name.Name);
            }
        });

        if (namesInRoom.Count == 0)
        {
            await session.WriteLineAsync("  Nothing special.");
        }
        else
        {
            foreach (var name in namesInRoom)
            {
                await session.WriteLineAsync($"- {name}");
            }
        }
    }
}