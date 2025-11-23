using System;
using System.Threading.Tasks;
using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;

namespace MUD.Telnet.Commands
{
    public class Attack : ICommand
    {
        public string Name => "attack";

        public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
        {
            if (!session.PlayerEntity.HasValue) return;
            var player = session.PlayerEntity.Value;

            if (args.Length == 0)
            {
                await session.WriteLineAsync("Attack who?");
                return;
            }

            string targetName = args[0];

            // 1. Find Target in Room
            Entity targetEntity = Entity.Null;
            var playerLoc = world.Get<LocationComponent>(player);

            var query = new QueryDescription().WithAll<NameComponent, LocationComponent, VitalsComponent>();
            world.Query(in query, (Entity e, ref NameComponent name, ref LocationComponent loc) =>
            {
                if (e != player && loc.RoomId == playerLoc.RoomId &&
                    name.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    targetEntity = e;
                }
            });

            if (targetEntity == Entity.Null)
            {
                await session.WriteLineAsync("You don't see them here.");
                return;
            }

            // 2. Check if already fighting
            if (world.Has<InCombatComponent>(player))
            {
                // If already in combat, this queues an attack for YOUR turn
                if (world.Has<AttackActionComponent>(player))
                    world.Remove<AttackActionComponent>(player);

                world.Add(player, new AttackActionComponent { Target = targetEntity });
                await session.WriteLineAsync($"You focus your attack on the {targetName}!");
            }
            else
            {
                // 3. Start New Combat
                world.Add(player, new StartCombatRequestComponent { Target = targetEntity });
                await session.WriteLineAsync($"You lunge at the {targetName}! Rolling initiative...");
            }
        }
    }
}