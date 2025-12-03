using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;

namespace MUD.Rulesets.D20.GameSystems
{
    public class RecoverySystem : ISystem<GameTime>
    {
        private readonly World _world;

        public RecoverySystem(World world)
        {
            _world = world;
        }

        public void Update(in GameTime gameTime)
        {
            var dt = gameTime.Elapsed;

            // Query specifically for Unconscious people who are Waiting (have a timer)
            var query = new QueryDescription().WithAll<UnconsciousComponent, ReviveTimerComponent, OutputMessageComponent>();

            _world.Query(in query, (Entity entity, ref ReviveTimerComponent timer, ref OutputMessageComponent output) =>
            {
                timer.TimeRemaining -= dt;

                // Notify every 10 seconds (roughly) just so they know it's working
                if (Math.Abs(timer.TimeRemaining % 10) < dt)
                {
                    output.Messages.Add($"... recovering ... ({timer.TimeRemaining:F0}s remaining)");
                }

                if (timer.TimeRemaining <= 0)
                {
                    // TIME IS UP!
                    _world.Remove<ReviveTimerComponent>(entity);

                    // Trigger the Wake Up Logic
                    WakeUp(entity);
                    output.Messages.Add("You gasp for air. You are awake.");
                }
            });
        }

        private void WakeUp(Entity entity)
        {
            // 1. Remove Status
            _world.Remove<UnconsciousComponent>(entity);

            // 2. Heal to 1 HP (or minimal amount)
            if (_world.Has<VitalsComponent>(entity))
            {
                ref var vitals = ref _world.Get<VitalsComponent>(entity);
                vitals.CurrentHP = 1; // Just barely alive
            }

            // 3. Teleport to Anchor
            if (_world.Has<RespawnAnchorComponent>(entity))
            {
                var anchor = _world.Get<RespawnAnchorComponent>(entity);
                if (_world.Has<LocationComponent>(entity))
                {
                    ref var loc = ref _world.Get<LocationComponent>(entity);
                    loc.RoomId = anchor.RoomId;
                    loc.X = anchor.X;
                    loc.Y = anchor.Y;
                }
            }
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}