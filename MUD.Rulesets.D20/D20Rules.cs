using MUD.Rulesets.D20.Components;

namespace MUD.Rulesets.D20
{
    public static class D20Rules
    {
        /// <summary>
        /// Calculates the D20 ability score modifier for a given score.
        /// The formula is (score - 10) / 2, rounded down.
        /// </summary>
        public static int GetAbilityModifier(int score)
        {
            // We cast to a double to ensure floating-point division,
            // then use Math.Floor to correctly round down before
            // casting back to an int.
            return (int)Math.Floor((score - 10) / 2.0);
        }

        /// <summary>
        /// Gets the governing ability score for a given skill.
        /// </summary>
        public static int GetGoverningAbilityScore(Skill skill, in CoreStatsComponent stats)
        {
            return skill switch
            {
                Skill.Acrobatics => stats.Dexterity,
                Skill.Stealth => stats.Dexterity,
                Skill.Perception => stats.Wisdom,
                Skill.Diplomacy => stats.Charisma,
                _ => 10,
            };
        }
        /// <summary>
        /// Gets the correct skill rank from the SkillsComponent.
        /// </summary>
        public static int GetSkillRank(in SkillsComponent skills, Skill skill)
        {
            return skill switch
            {
                Skill.Acrobatics => skills.Acrobatics,
                Skill.Perception => skills.Perception,
                Skill.Stealth => skills.Stealth,
                Skill.Diplomacy => skills.Diplomacy,
                _ => 0,
            };
        }
    }
}