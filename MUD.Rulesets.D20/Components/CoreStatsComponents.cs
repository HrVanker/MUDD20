namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// A component that stores the six core D20 ability scores for an entity.
    /// This is a struct for performance, as components are pure data.
    /// </summary>
    public struct CoreStatsComponent
    {
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
    }
}