using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Collections.Generic;

namespace MUD.Rulesets.D20.GameSystems
{
    public class SkillCheckSystem : ISystem<GameTime>
    {
        private readonly World _world;
        private readonly Random _random = new Random();

        public SkillCheckSystem(World world, GameState gameState)
        {
            _world = world;
        }

        public void Update(in GameTime gameTime)
        {
            var query = new QueryDescription().WithAll<SkillCheckRequestComponent>();
            var entitiesToDestroy = new List<Entity>();

            _world.Query(in query, (Entity entity, ref SkillCheckRequestComponent request) =>
            {
                // Make sure the performing entity is still valid.
                if (!_world.IsAlive(request.Performer))
                {
                    entitiesToDestroy.Add(entity);
                    return;
                }

                // Get the performer's components.
                var performerName = _world.Get<NameComponent>(request.Performer).Name;
                var performerStats = _world.Get<CoreStatsComponent>(request.Performer);
                var performerSkills = _world.Get<SkillsComponent>(request.Performer);

                // 1. Roll the d20.
                int diceRoll = _random.Next(1, 21);

                // 2. Get the skill rank and ability modifier.
                int skillRank = GetSkillRank(performerSkills, request.Skill);
                int abilityModifier = GetAbilityModifier(performerStats, request.Skill);

                // 3. Calculate the total result.
                int totalResult = diceRoll + skillRank + abilityModifier;

                // 4. Determine the outcome.
                string outcome;
                if (diceRoll == 1) outcome = "Critical Failure!";
                else if (diceRoll == 20) outcome = "Critical Success!";
                else if (totalResult >= request.DifficultyClass) outcome = "Success!";
                else outcome = "Failure.";

                Console.WriteLine($"--- SKILL CHECK: {request.Skill} ---");
                Console.WriteLine($"  {performerName} attempts a DC {request.DifficultyClass} check.");
                Console.WriteLine($"  Roll: {diceRoll} + Skill({skillRank}) + Mod({abilityModifier}) = {totalResult}");
                Console.WriteLine($"  Outcome: {outcome}");
                Console.WriteLine("--------------------------");

                entitiesToDestroy.Add(entity);
            });

            foreach (var entity in entitiesToDestroy) { _world.Destroy(entity); }
        }

        // A helper method to get the correct skill rank from the component.
        private int GetSkillRank(in SkillsComponent skills, Skill skill)
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

        // A helper method to calculate the D20 ability score modifier.
        private int GetAbilityModifier(in CoreStatsComponent stats, Skill skill)
        {
            int score = skill switch
            {
                Skill.Acrobatics => stats.Dexterity,
                Skill.Stealth => stats.Dexterity,
                Skill.Perception => stats.Wisdom,
                Skill.Diplomacy => stats.Charisma,
                _ => 10,
            };
            return (score - 10) / 2; // This is the standard D20 formula.
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}