using Arch.Core;
using Arch.System;
using MUD.Core;
using System;

namespace MUD.Rulesets.D20.GameSystems
{
    public class HelloWorldSystem : ISystem<GameTime>
    {
        private readonly World _world;
        private readonly GameState _gameState;

        public HelloWorldSystem(World world, GameState gameState)
        {
            _world = world;
            _gameState = gameState;
        }

        public void Update(in GameTime gameTime)
        {
            Console.WriteLine("Hello from the ECS System! The game is running.");
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}