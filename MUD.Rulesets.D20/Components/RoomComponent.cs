using Arch.Core;
using System.Collections.Generic;

namespace MUD.Rulesets.D20.Components
{
    public struct RoomComponent
    {
        public string Title;
        public string Description;
        public int AreaId;

        // --- NEW: Tactical Map Dimensions ---
        // Default to 10x10 squares (50ft x 50ft) if not set
        public int Width;
        public int Height;

        // Directions to other rooms (e.g., "north" -> 1002)
        public Dictionary<string, int> Exits;
    }
}