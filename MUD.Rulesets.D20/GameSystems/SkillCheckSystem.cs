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
        private readonly IDiceRoller _diceRoller;

        public SkillCheckSystem(World world, GameState gameState, IDiceRoller diceRoller)
        {
            _world = world;
            _diceRoller = diceRoller;
        }

        public void Update(in GameTime gameTime)
        {
            var query = new QueryDescription().WithAll<SkillCheckRequestComponent>();
            var entitiesToDestroy = new List<Entity>();

            _world.Query(in query, (Entity entity, ref SkillCheckRequestComponent request) =>
            {
                if (!_world.IsAlive(request.Performer))
                {
                    entitiesToDestroy.Add(entity);
                    return;
                }

                // --- ALL LOGIC MUST BE INSIDE THIS LAMBDA ---

                var performerName = _world.Get<NameComponent>(request.Performer).Name;
                var performerStats = _world.Get<CoreStatsComponent>(request.Performer);
                var performerSkills = _world.Get<SkillsComponent>(request.Performer);

                // 1. Roll the d20.
                int diceRoll = _diceRoller.Roll(20);

                // 2. Get skill rank and ability modifier using the D20Rules helper class.
                int skillRank = D20Rules.GetSkillRank(performerSkills, request.Skill);
                int abilityScore = D20Rules.GetGoverningAbilityScore(request.Skill, performerStats);
                int abilityModifier = D20Rules.GetAbilityModifier(abilityScore);

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

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}