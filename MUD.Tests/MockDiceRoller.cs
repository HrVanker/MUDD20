using MUD.Core;

namespace MUD.Tests
{
    /// <summary>
    /// A special dice roller for testing that can be told what number to return.
    /// </summary>
    public class MockDiceRoller : IDiceRoller
    {
        public int NextRoll { get; set; } = 20; // Default to 1

        public int Roll(int sides)
        {
            return NextRoll;
        }
    }
}