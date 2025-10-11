using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components; // <-- Need this for the component
using System;

namespace MUD.Rulesets.D20.GameSystems
{
    public class CharacterSheetSystem : ISystem<GameTime>
    {
        private readonly World _world;
        private readonly GameState _gameState;

        public CharacterSheetSystem(World world, GameState gameState)
        {
            _world = world;
            _gameState = gameState;
        }

        public void Update(in GameTime gameTime)
        {
            // Update the query to look for entities with BOTH components
            var query = new QueryDescription().WithAll<CoreStatsComponent, NameComponent>();

            // Update the lambda to receive the new component
            _world.Query(in query, (Entity entity, ref CoreStatsComponent stats, ref NameComponent name) =>
            {
                Console.WriteLine("--- Character Sheet ---");
                Console.WriteLine($"  Name: {name.Name} (Entity ID: {entity.Id})"); // <-- Updated line
                Console.WriteLine($"  Strength:     {stats.Strength}");
                Console.WriteLine($"  Dexterity:    {stats.Dexterity}");
                Console.WriteLine($"  Constitution: {stats.Constitution}");
                Console.WriteLine($"  Intelligence: {stats.Intelligence}");
                Console.WriteLine($"  Wisdom:       {stats.Wisdom}");
                Console.WriteLine($"  Charisma:     {stats.Charisma}");
                Console.WriteLine("-----------------------");
            });
        }

        // ... (empty Initialize, BeforeUpdate, AfterUpdate, Dispose methods) ...
        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}