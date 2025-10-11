using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;

namespace MUD.Rulesets.D20.GameSystems
{
    public class DebugStatsSystem : ISystem<GameTime>
    {
        private readonly World _world;
        public DebugStatsSystem(World world, GameState gs) { _world = world; }

        public void Update(in GameTime gameTime)
        {
            var query = new QueryDescription().WithAll<CoreStatsComponent>();
            _world.Query(in query, (Entity entity) =>
            {
                Console.WriteLine($"[Debug] Found Entity with Stats, ID: {entity.Id}");
            });
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}