using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;

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
            Entity targetEntity = Entity.Null;
            var playerLoc = world.Get<LocationComponent>(player);

            // 1. Find Target
            var query = new QueryDescription().WithAll<NameComponent, LocationComponent, VitalsComponent>();
            world.Query(in query, (Entity entity, ref NameComponent name, ref LocationComponent loc) =>
            {
                if (entity != player &&
                    loc.RoomId == playerLoc.RoomId &&
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

            // 2. Check Distance (Chebyshev: Max(dx, dy))
            var targetLoc = world.Get<LocationComponent>(targetEntity);
            int distance = Math.Max(Math.Abs(playerLoc.X - targetLoc.X), Math.Abs(playerLoc.Y - targetLoc.Y));

            if (distance > 1)
            {
                // --- TOO FAR: MOVE INSTEAD ---
                await session.WriteLineAsync($"The {targetName} is too far away ({distance} sq). Moving closer...");

                // Clear any old move requests
                if (world.Has<MoveToRequestComponent>(player)) world.Remove<MoveToRequestComponent>(player);

                // Add request to move to the target's coordinates
                world.Add(player, new MoveToRequestComponent
                {
                    TargetX = targetLoc.X,
                    TargetY = targetLoc.Y
                });

                return; // End command here. Player will move, then they can type 'attack' again.
            }

            // 3. WITHIN RANGE: ATTACK
            if (world.Has<InCombatComponent>(player))
            {
                // Queue specific action for this turn
                if (world.Has<AttackActionComponent>(player)) world.Remove<AttackActionComponent>(player);
                world.Add(player, new AttackActionComponent { Target = targetEntity });
                await session.WriteLineAsync($"You focus your attack on the {targetName}!");
            }
            else
            {
                // Start Combat
                var combatants = new List<Entity> { player, targetEntity };
                world.Add(player, new StartCombatRequestComponent { Combatants = combatants });
                await session.WriteLineAsync($"You prepare to attack the {targetName}! Rolling initiative...");
            }
        }
    }
}