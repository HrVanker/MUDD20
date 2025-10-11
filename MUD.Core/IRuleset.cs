using Arch.Core;
using Arch.System;

namespace MUD.Core
{
    // We still need our own simple GameTime for the loop
    public readonly struct GameTime
    {
        public readonly float Elapsed;
        public GameTime(float elapsed) => Elapsed = elapsed;
    }

    public class GameState { }

    public interface IRuleset
    {
        string Name { get; }
        void LoadContent(World ecsWorld, string worldModulePath);
        Group<GameTime> RegisterSystems(World ecsWorld, GameState gameState);
    }
}