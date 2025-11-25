using System;
using System.Collections.Generic; // Needed for List<>
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

            // 1. Find the target in the same room
            Entity targetEntity = Entity.Null;
            var playerLocation = world.Get<LocationComponent>(player);

            // Note: We include VitalsComponent to ensure we only attack living things
            var query = new QueryDescription().WithAll<NameComponent, LocationComponent, VitalsComponent>();

            world.Query(in query, (Entity entity, ref NameComponent name, ref LocationComponent loc) =>
            {
                if (entity != player &&
                    loc.RoomId == playerLocation.RoomId &&
                    name.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    targetEntity = entity;
                }
            });

            if (targetEntity == Entity.Null)
            {
                await session.WriteLineAsync("You don't see them here.");
                return;
            }

            // 2. Check if already in combat
            if (world.Has<InCombatComponent>(player))
            {
                // If already in combat, queue a specific attack action for this turn
                if (world.Has<AttackActionComponent>(player))
                    world.Remove<AttackActionComponent>(player);

                world.Add(player, new AttackActionComponent { Target = targetEntity });
                await session.WriteLineAsync($"You focus your attack on the {targetName}!");
            }
            else
            {
                // 3. Start Combat (FIXED)
                // Your system expects a List<Entity> containing all participants.
                var combatants = new List<Entity> { player, targetEntity };

                // Attach the request with the correct list
                world.Add(player, new StartCombatRequestComponent { Combatants = combatants });

                await session.WriteLineAsync($"You prepare to attack the {targetName}! Rolling initiative...");
            }
        }
    }
}