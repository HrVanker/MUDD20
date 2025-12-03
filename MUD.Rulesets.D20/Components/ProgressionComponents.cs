using Arch.Core;

namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// Tracks a character's progression.
    /// </summary>
    public struct ExperienceComponent
    {
        public int CurrentXP;
        public int Level;
        public int NextLevelXP => Level * 1000; // Simple D&D style: 1000, 2000, 3000...
    }

    /// <summary>
    /// Defines how much XP this entity is worth when defeated.
    /// </summary>
    public struct XpRewardComponent
    {
        public int Amount;
    }
}