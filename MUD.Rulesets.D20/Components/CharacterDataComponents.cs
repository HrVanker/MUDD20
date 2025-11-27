using Arch.Core;

namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// Stores the health and other resource pools for an entity.
    /// </summary>
    public struct VitalsComponent
    {
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int TempHP { get; set; }
    }

    /// <summary>
    /// Stores an entity's base armor class and other combat-related stats.
    /// </summary>
    public struct CombatStatsComponent
    {
        public int ArmorClass { get; set; }
        public int BaseAttackBonus { get; set; }
        // We can add more here later, like different save types (Fortitude, Reflex, Will)
    }

    /// <summary>
    /// Stores the skill ranks for a character.
    /// For simplicity, we'll start with just a few skills.
    /// </summary>
    public struct SkillsComponent
    {
        public int Acrobatics { get; set; }
        public int Perception { get; set; }
        public int Stealth { get; set; }
        public int Diplomacy { get; set; }
    }

    /// <summary>
    /// A component added to an entity when it enters combat.
    /// Stores its initiative roll and current status.
    /// </summary>
    public struct InCombatComponent
    {
        public int Initiative;
        // We could add status effects here later (e.g., Stunned, Prone)
    }

    /// <summary>
    /// A "singleton" component, where only one exists in the world.
    /// It tracks the overall state of the current combat encounter.
    /// </summary>
    public struct CombatTurnComponent
    {
        // A list of all entities in combat, sorted by initiative.
        public List<Entity> TurnOrder;

        // The index in the TurnOrder list of the entity whose turn it currently is.
        public int CurrentTurnIndex;

        public int RoundNumber;
    }

    // Tracks current gold
    public struct MoneyComponent
    {
        public int Amount;
    }

    // Tracks religious allegiance (for flavor text)
    public struct DeityComponent
    {
        public string DeityName; // e.g., "Crom", "The Light", "None"
    }

    // Tracks where the player revives (set this on Rest)
    public struct RespawnAnchorComponent
    {
        public int RoomId;
        public int X;
        public int Y;
    }

    // If they choose to wait, this tracks the time remaining
    public struct ReviveTimerComponent
    {
        public float TimeRemaining; // Seconds
    }
}