using System;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;

namespace MUD.Telnet.Commands
{
    public class MoveCommand : ICommand
    {
        public string Name => "move";

        public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
        {
            if (!session.PlayerEntity.HasValue) return;
            var player = session.PlayerEntity.Value;

            if (args.Length == 0)
            {
                await session.WriteLineAsync("Move where? (e.g., 'north', '5,5', 'to goblin')");
                return;
            }

            // We need the player's current location to calculate the target
            if (!world.Has<LocationComponent>(player))
            {
                await session.WriteLineAsync("You are floating in the void.");
                return;
            }

            var playerLoc = world.Get<LocationComponent>(player);
            int targetX = playerLoc.X;
            int targetY = playerLoc.Y;
            bool validMove = false;

            string firstArg = args[0].ToLower();

            // --- OPTION 1: Move To Entity ("move to goblin") ---
            if (firstArg == "to" && args.Length > 1)
            {
                string targetName = string.Join(" ", args.Skip(1)); // Rejoin remaining args
                Entity targetEntity = Entity.Null;

                // Query world for entity with matching Name in the SAME ROOM
                var query = new QueryDescription().WithAll<NameComponent, LocationComponent>();
                world.Query(in query, (Entity e, ref NameComponent name, ref LocationComponent loc) =>
                {
                    if (loc.RoomId == playerLoc.RoomId &&
                        name.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetEntity = e;
                    }
                });

                if (targetEntity != Entity.Null)
                {
                    var targetLoc = world.Get<LocationComponent>(targetEntity);
                    targetX = targetLoc.X;
                    targetY = targetLoc.Y;
                    validMove = true;

                    // Optional: Don't move if we are already on top of them
                    if (targetX == playerLoc.X && targetY == playerLoc.Y)
                    {
                        await session.WriteLineAsync($"You are already at the {targetName}'s location.");
                        return;
                    }
                    await session.WriteLineAsync($"Moving towards {targetName} at ({targetX},{targetY})...");
                }
                else
                {
                    await session.WriteLineAsync($"You don't see '{targetName}' here.");
                    return;
                }
            }
            // --- OPTION 2: Move by Coordinates ("move 5,5") ---
            else if (firstArg.Contains(","))
            {
                var parts = firstArg.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int x) &&
                    int.TryParse(parts[1], out int y))
                {
                    targetX = x;
                    targetY = y;
                    validMove = true;
                    await session.WriteLineAsync($"Moving to coordinates ({x},{y})...");
                }
                else
                {
                    await session.WriteLineAsync("Invalid format. Use x,y (e.g., 'move 5,5')");
                    return;
                }
            }
            // --- OPTION 3: Move by Direction ("move north") ---
            else
            {
                int distance = 1;
                if (args.Length > 1) int.TryParse(args[1], out distance);

                switch (firstArg)
                {
                    case "n": case "north": targetY -= distance; break;
                    case "s": case "south": targetY += distance; break;
                    case "e": case "east": targetX += distance; break;
                    case "w": case "west": targetX -= distance; break;
                    case "ne": targetY -= distance; targetX += distance; break;
                    case "nw": targetY -= distance; targetX -= distance; break;
                    case "se": targetY += distance; targetX += distance; break;
                    case "sw": targetY += distance; targetX -= distance; break;
                    default:
                        await session.WriteLineAsync("Unknown direction.");
                        return;
                }
                validMove = true;
                await session.WriteLineAsync($"Moving {firstArg}...");
            }

            // --- EXECUTE: Send Request to ECS ---
            if (validMove)
            {
                // Clean up any old requests first
                if (world.Has<MoveToRequestComponent>(player))
                    world.Remove<MoveToRequestComponent>(player);

                // Add the request. The MovementSystem will pick this up next Tick.
                world.Add(player, new MoveToRequestComponent
                {
                    TargetX = targetX,
                    TargetY = targetY
                });
            }
        }
    }
}