using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;

namespace MUD.Rulesets.D20.GameSystems
{
    public class DebugNameSystem : ISystem<GameTime>
    {
        private readonly World _world;
        public DebugNameSystem(World world, GameState gs) { _world = world; }

        public void Update(in GameTime gameTime)
        {
            var query = new QueryDescription().WithAll<NameComponent>();
            _world.Query(in query, (ref NameComponent name) =>
            {
                Console.WriteLine($"[Debug] Found Entity with Name: {name.Name}");
            });
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}