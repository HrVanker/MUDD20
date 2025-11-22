namespace MUD.Rulesets.D20.Components
{
    public struct LocationComponent
    {
        // For now, we'll use a simple integer to represent a "room".
        // Room 0 will be the starting area.
        public int RoomId;

        public int X;
        public int Y;
    }
}