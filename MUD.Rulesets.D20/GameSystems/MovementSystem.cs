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
            var query = new QueryDescription()
                .WithAll<LocationComponent, MoveToRequestComponent>()
                .WithNone<UnconsciousComponent, DeadComponent>();

            _world.Query(in query, (Entity entity, ref LocationComponent loc, ref MoveToRequestComponent request) =>
            {
                var roomEntity = GetRoomEntity(loc.RoomId);
                if (roomEntity == Entity.Null) return;

                var room = _world.Get<RoomComponent>(roomEntity);

                // 1. Check for Exit / Zone Transition
                // If the target is outside bounds, we check if that direction has an exit.
                string exitDirection = null;

                if (request.TargetY < 0) exitDirection = "north";
                else if (request.TargetY >= room.Height) exitDirection = "south";
                else if (request.TargetX < 0) exitDirection = "west";
                else if (request.TargetX >= room.Width) exitDirection = "east";

                if (exitDirection != null)
                {
                    if (room.Exits.TryGetValue(exitDirection, out int targetRoomId))
                    {
                        // TRANSITION!
                        Console.WriteLine($"Transitioning {exitDirection} to Room {targetRoomId}...");

                        // Get dimensions of the NEW room to calculate entry point
                        var targetRoomEntity = GetRoomEntity(targetRoomId);
                        if (targetRoomEntity != Entity.Null)
                        {
                            var targetRoom = _world.Get<RoomComponent>(targetRoomEntity);

                            // Update Room ID
                            loc.RoomId = targetRoomId;

                            // Calculate new coordinates (entering from opposite side)
                            if (exitDirection == "north") { loc.X = Math.Clamp(loc.X, 0, targetRoom.Width - 1); loc.Y = targetRoom.Height - 1; }
                            else if (exitDirection == "south") { loc.X = Math.Clamp(loc.X, 0, targetRoom.Width - 1); loc.Y = 0; }
                            else if (exitDirection == "east") { loc.X = 0; loc.Y = Math.Clamp(loc.Y, 0, targetRoom.Height - 1); }
                            else if (exitDirection == "west") { loc.X = targetRoom.Width - 1; loc.Y = Math.Clamp(loc.Y, 0, targetRoom.Height - 1); }

                            // Clear request and return so we don't try to pathfind in the old room
                            _world.Remove<MoveToRequestComponent>(entity);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Blocked: No exit in that direction.");
                        // Clamp target to edge so they walk up to the wall but stop
                        request.TargetX = Math.Clamp(request.TargetX, 0, room.Width - 1);
                        request.TargetY = Math.Clamp(request.TargetY, 0, room.Height - 1);
                    }
                }

                // 2. Standard Pathfinding (Same as before)
                var start = new Point(loc.X, loc.Y);
                Point target = new Point(request.TargetX, request.TargetY);

                var path = Pathfinder.FindPath(start, target, room.Width, room.Height);

                if (path != null && path.Any())
                {
                    int speed = 6;
                    if (_world.Has<CoreStatsComponent>(entity)) { /* speed logic */ }

                    var stepsToTake = path.Take(speed).ToList();
                    var finalStep = stepsToTake.Last();

                    loc.X = finalStep.X;
                    loc.Y = finalStep.Y;

                    if (finalStep.X == target.X && finalStep.Y == target.Y)
                    {
                        // We reached the target spot
                    }
                }

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