using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Linq;

namespace MUD.Rulesets.D20.GameSystems
{
    public class MovementSystem : ISystem<GameTime>
    {
        private readonly World _world;

        public MovementSystem(World world)
        {
            _world = world;
        }

        public void Update(in GameTime gameTime)
        {
            // Query for entities that want to move
            var query = new QueryDescription().WithAll<LocationComponent, MoveToRequestComponent>();

            _world.Query(in query, (Entity entity, ref LocationComponent loc, ref MoveToRequestComponent request) =>
            {
                // 1. Get the room dimensions (assuming we can find the room entity by ID)
                // For MVP, we will look up the Room Entity based on the Player's RoomID
                var roomEntity = GetRoomEntity(loc.RoomId);
                if (roomEntity == Entity.Null) return;

                var room = _world.Get<RoomComponent>(roomEntity);

                // 2. Define Start and Target
                var start = new Point(loc.X, loc.Y);
                Point target = new Point(request.TargetX, request.TargetY);

                // 3. Calculate Path
                var path = Pathfinder.FindPath(start, target, room.Width, room.Height);

                if (path != null && path.Any())
                {
                    // 4. Determine how far we can travel
                    // Default to 6 squares (30ft) if no stats. 
                    int speed = 6;
                    if (_world.Has<CoreStatsComponent>(entity))
                    {
                        // Could grab actual Speed stat here later
                    }

                    // Take only as many steps as our speed allows
                    var stepsToTake = path.Take(speed).ToList();
                    var finalStep = stepsToTake.Last();

                    // 5. Update Location
                    loc.X = finalStep.X;
                    loc.Y = finalStep.Y;

                    Console.WriteLine($"Entity moved {stepsToTake.Count} steps to {finalStep}.");

                    // 6. Check if we reached the destination or just moved closer
                    if (finalStep.X == target.X && finalStep.Y == target.Y)
                    {
                        Console.WriteLine("Target reached.");
                        // If this was a "Take" command, we could trigger an interaction here
                    }
                    else
                    {
                        Console.WriteLine("Movement ran out before reaching destination.");
                    }
                }
                else
                {
                    Console.WriteLine("No path found to target!");
                }

                // Request complete
                _world.Remove<MoveToRequestComponent>(entity);
            });
        }

        // Helper to find the room entity. 
        // In a real ECS, you might maintain a Dictionary<int, Entity> cache for O(1) lookup.
        private Entity GetRoomEntity(int roomId)
        {
            var q = new QueryDescription().WithAll<RoomComponent>();
            Entity found = Entity.Null;

            _world.Query(in q, (Entity e, ref RoomComponent r) =>
            {
                if (r.AreaId == roomId) found = e;
            });

            return found;
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }

    // Helper Component to trigger movement
    public struct MoveToRequestComponent
    {
        public int TargetX;
        public int TargetY;
        // Optional: TargetEntity if following someone
    }
}