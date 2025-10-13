﻿namespace MUD.Rulesets.D20.Components
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
}