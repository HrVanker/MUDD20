using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System.Threading.Tasks;

public class RestCommand : ICommand
{
    public string Name => "rest";

    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;
        var player = session.PlayerEntity.Value;

        // 1. Check if in Combat
        if (world.Has<InCombatComponent>(player))
        {
            await session.WriteLineAsync("You cannot rest while in combat!");
            return;
        }

        // 2. Perform Rest: Heal to Max
        int newHp = 0;
        if (world.Has<VitalsComponent>(player))
        {
            var vitals = world.Get<VitalsComponent>(player);
            vitals.CurrentHP = vitals.MaxHP;
            world.Set(player, vitals);
        }

        // 3. Update Respawn Anchor to current location
        int roomId = 0, x = 0, y = 0;
        if (world.Has<LocationComponent>(player))
        {
            var loc = world.Get<LocationComponent>(player);
            roomId = loc.RoomId;
            x = loc.X;
            y = loc.Y;

            if (world.Has<RespawnAnchorComponent>(player))
            {
                var anchor = world.Get<RespawnAnchorComponent>(player);
                anchor.RoomId = roomId;
                anchor.X = x;
                anchor.Y = y;
                world.Set(player, anchor);
            }
        }

        await session.WriteLineAsync("You rest for a while, tending to your wounds...");
        await session.WriteLineAsync("You feel fully recovered and have established a new respawn point.");

        // 4. Persistence: Save to Database
        if (roomId > 0)
        {
            session.DbService.SavePlayerState(session.AccountId, roomId, x, y, newHp);
            await session.WriteLineAsync("[Game Saved]");
        }
    }
}