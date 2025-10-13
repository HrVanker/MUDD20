using Arch.Core;

namespace MUD.Rulesets.D20.Components
{
    // A mapping of skills to their governing ability scores.
    public enum Skill
    {
        Acrobatics, // Dexterity
        Perception, // Wisdom
        Stealth,    // Dexterity
        Diplomacy   // Charisma
    }

    /// <summary>
    /// An event component that requests a skill check be resolved for an entity.
    /// </summary>
    public struct SkillCheckRequestComponent
    {
        // The entity performing the skill check.
        public Entity Performer;

        // The skill being used.
        public Skill Skill;

        // The Difficulty Class of the check.
        public int DifficultyClass;
    }
}