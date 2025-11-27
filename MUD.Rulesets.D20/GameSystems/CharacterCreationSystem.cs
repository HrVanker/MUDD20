using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace MUD.Rulesets.D20.GameSystems
{
    public class CharacterCreationSystem
    {
        private readonly World _world;
        private readonly IDatabaseService _dbService;

        public CharacterCreationSystem(World world, IDatabaseService dbService)
        {
            _world = world;
            _dbService = dbService;
        }

        public Entity? Update(in GameTime gameTime)
        {
            Entity? createdEntity = null;
            var query = new QueryDescription().WithAll<PlayerLoginRequestComponent>();
            var entitiesToDestroy = new List<Entity>();

            _world.Query(in query, (Entity entity, ref PlayerLoginRequestComponent request) =>
            {
                Console.WriteLine($"Processing login for Account ID: {request.AccountId}...");
                var savedPlayer = _dbService.GetPlayerRecord(request.AccountId);
                if (savedPlayer == null)
                {
                    Console.WriteLine($"Error: No player record found for Account ID {request.AccountId}.");
                    entitiesToDestroy.Add(entity);
                    return;
                }

                var baseTemplate = LoadTemplate($"Content/Aethelgard/creatures/player.toml");
                var raceTemplate = LoadTemplate($"Content/Aethelgard/races/{savedPlayer.Race}.toml");
                var classTemplate = LoadTemplate($"Content/Aethelgard/classes/{savedPlayer.Class}.toml");

                if (baseTemplate == null || raceTemplate == null || classTemplate == null)
                {
                    Console.WriteLine("Error: Missing a required character template file.");
                    entitiesToDestroy.Add(entity);
                    return;
                }

                var playerEntity = _world.Create();
                _world.Add(playerEntity, new NameComponent { Name = savedPlayer.CharacterName });
                _world.Add(playerEntity, new InventoryComponent { Items = new List<Entity>() });
                _world.Add(playerEntity, new EquipmentComponent());
                _world.Add(playerEntity, new LocationComponent { RoomId = 100, X = 0, Y = 0 });
                _world.Add(playerEntity, new MoneyComponent { Amount = 50 }); // Start with 50 gold
                _world.Add(playerEntity, new DeityComponent { DeityName = "Crom" }); // Default deity for testing
                _world.Add(playerEntity, new RespawnAnchorComponent { RoomId = 1, X = 0, Y = 0 }); // Default to start room

                var stats = ParseComponent<CoreStatsComponent>(baseTemplate, "stats");
                var raceMods = (TomlTable)raceTemplate["stat_modifiers"];
                stats.Strength += Convert.ToInt32(raceMods["strength"]);
                stats.Dexterity += Convert.ToInt32(raceMods["dexterity"]);
                stats.Constitution += Convert.ToInt32(raceMods["constitution"]);
                stats.Intelligence += Convert.ToInt32(raceMods["intelligence"]);
                stats.Wisdom += Convert.ToInt32(raceMods["wisdom"]);
                stats.Charisma += Convert.ToInt32(raceMods["charisma"]);
                _world.Add(playerEntity, stats);

                _world.Add(playerEntity, ParseComponent<VitalsComponent>(classTemplate, "vitals"));
                _world.Add(playerEntity, ParseComponent<CombatStatsComponent>(classTemplate, "combat"));
                _world.Add(playerEntity, ParseComponent<SkillsComponent>(classTemplate, "skills"));

                Console.WriteLine($"Successfully created a {savedPlayer.Race} {savedPlayer.Class} named '{savedPlayer.CharacterName}'.");

                // --- THE FIX ---
                // This line captures the created entity so we can return it.
                createdEntity = playerEntity;
                // --- END FIX ---

                entitiesToDestroy.Add(entity);
            });

            foreach (var entity in entitiesToDestroy) { _world.Destroy(entity); }

            return createdEntity;
        }

        // Helper method to load any TOML file
        private TomlTable LoadTemplate(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            return Toml.ToModel(File.ReadAllText(filePath));
        }

        // Generic helper to parse a component from a TOML table
        private T ParseComponent<T>(TomlTable template, string key) where T : new()
        {
            var component = new T();
            if (template.TryGetValue(key, out var table) && table is TomlTable tomlTable)
            {
                // This is a simplified version. A real implementation might use reflection
                // or a more robust mapping library to set properties.
                if (component is CoreStatsComponent s)
                {
                    s.Strength = Convert.ToInt32(tomlTable["strength"]);
                    s.Dexterity = Convert.ToInt32(tomlTable["dexterity"]);
                    s.Constitution = Convert.ToInt32(tomlTable["constitution"]);
                    s.Intelligence = Convert.ToInt32(tomlTable["intelligence"]);
                    s.Wisdom = Convert.ToInt32(tomlTable["wisdom"]);
                    s.Charisma = Convert.ToInt32(tomlTable["charisma"]);
                    return (T)(object)s;
                }
                if (component is VitalsComponent v)
                {
                    v.MaxHP = Convert.ToInt32(tomlTable["max_hp"]);
                    v.CurrentHP = v.MaxHP; // Start at full health
                    return (T)(object)v;
                }
                if (component is CombatStatsComponent c)
                {
                    c.BaseAttackBonus = Convert.ToInt32(tomlTable["base_attack_bonus"]);
                    return (T)(object)c;
                }
                if (component is SkillsComponent sk)
                {
                    sk.Acrobatics = Convert.ToInt32(tomlTable["acrobatics"]);
                    sk.Perception = Convert.ToInt32(tomlTable["perception"]);
                    sk.Stealth = Convert.ToInt32(tomlTable["stealth"]);
                    sk.Diplomacy = Convert.ToInt32(tomlTable["diplomacy"]);
                    return (T)(object)sk;
                }
            }
            return component;
        }

        public void Initialize() { }
        public void BeforeUpdate(in GameTime t) { }
        public void AfterUpdate(in GameTime t) { }
        public void Dispose() { }
    }
}