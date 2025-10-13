using Arch.Core;
using Arch.System;
using MUD.Core;
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System;
using System.Collections.Generic; // Needed for List
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace MUD.Rulesets.D20
{


    public class D20Ruleset : IRuleset
    {
        public string Name => "D20 Ruleset";

        public void LoadContent(World ecsWorld, string worldModulePath)
        {
            Console.WriteLine($"Loading world module from: {worldModulePath}");

            string manifestPath = Path.Combine(worldModulePath, "world.toml");
            if (!File.Exists(manifestPath))
            {
                Console.WriteLine($"Error: World manifest not found at {manifestPath}");
                return;
            }

            try
            {
                // 1. Read and parse the manifest file.
                string manifestContent = File.ReadAllText(manifestPath);
                var manifest = Toml.ToModel(manifestContent);

                // 2. Get the list of creature files from the manifest.
                if (manifest.TryGetValue("creatures", out var creatureFilesObj) && creatureFilesObj is TomlArray creatureFilesArray)
                {
                    Console.WriteLine($"Found {creatureFilesArray.Count} creature(s) to load from manifest.");
                    foreach (var creatureFile in creatureFilesArray)
                    {
                        // Build the full path to the creature file relative to the world module's root.
                        string relativePath = creatureFile.ToString();
                        string fullPath = Path.Combine(worldModulePath, relativePath);

                        // 3. Load each creature file specified.
                        LoadEntityFromFile(ecsWorld, fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing world manifest {manifestPath}: {ex.Message}");
            }
        }

        private void LoadEntityFromFile(World ecsWorld, string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);
                var tomlModel = Toml.ToModel(fileContent);

                // 1. Create an empty entity first.
                var entity = ecsWorld.Create();
                int componentsAdded = 0;

                // 2. Add components to the entity one by one.
                if (tomlModel.TryGetValue("name", out var nameValue))
                {
                    ecsWorld.Add(entity, new NameComponent { Name = nameValue.ToString() });
                    componentsAdded++;
                }

                if (tomlModel.TryGetValue("description", out var descValue))
                {
                    ecsWorld.Add(entity, new DescriptionComponent { Description = descValue.ToString() });
                    componentsAdded++;
                }

                if (tomlModel.TryGetValue("stats", out var statsValue) && statsValue is TomlTable statsTable)
                {
                    ecsWorld.Add(entity, new CoreStatsComponent
                    {
                        Strength = Convert.ToInt32(statsTable["strength"]),
                        Dexterity = Convert.ToInt32(statsTable["dexterity"]),
                        Constitution = Convert.ToInt32(statsTable["constitution"]),
                        Intelligence = Convert.ToInt32(statsTable["intelligence"]),
                        Wisdom = Convert.ToInt32(statsTable["wisdom"]),
                        Charisma = Convert.ToInt32(statsTable["charisma"])
                    });
                    componentsAdded++;
                }

                if (tomlModel.TryGetValue("vitals", out var vitalsValue) && vitalsValue is TomlTable vitalsTable)
                {
                    ecsWorld.Add(entity, new VitalsComponent
                    {
                        MaxHP = Convert.ToInt32(vitalsTable["max_hp"]),
                        CurrentHP = Convert.ToInt32(vitalsTable["current_hp"]),
                        TempHP = Convert.ToInt32(vitalsTable["temp_hp"])
                    });
                    componentsAdded++;
                }

                // CombatStats Component
                if (tomlModel.TryGetValue("combat", out var combatValue) && combatValue is TomlTable combatTable)
                {
                    ecsWorld.Add(entity, new CombatStatsComponent
                    {
                        ArmorClass = Convert.ToInt32(combatTable["armor_class"]),
                        BaseAttackBonus = Convert.ToInt32(combatTable["base_attack_bonus"])
                    });
                    componentsAdded++;
                }

                // Skills Component
                if (tomlModel.TryGetValue("skills", out var skillsValue) && skillsValue is TomlTable skillsTable)
                {
                    ecsWorld.Add(entity, new SkillsComponent
                    {
                        Acrobatics = Convert.ToInt32(skillsTable["acrobatics"]),
                        Perception = Convert.ToInt32(skillsTable["perception"]),
                        Stealth = Convert.ToInt32(skillsTable["stealth"]),
                        Diplomacy = Convert.ToInt32(skillsTable["diplomacy"])
                    });
                    componentsAdded++;
                }

                // If we found any components, create an entity with them.
                if (componentsAdded > 0)
                {
                    Console.WriteLine($"Successfully created entity from {Path.GetFileName(filePath)}.");
                }
                else
                { ecsWorld.Destroy(entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading entity from {filePath}: {ex.Message}");
            }
        }

        public Group<GameTime> RegisterSystems(World ecsWorld, GameState gameState)
        {
            Console.WriteLine("D20 Ruleset is registering systems...");
            var systems = new Group<GameTime>("D20GameSystems");
            systems.Add(new CharacterSheetSystem(ecsWorld, gameState));
            systems.Add(new SkillCheckSystem(ecsWorld, gameState));

            //systems.Add(new DebugNameSystem(ecsWorld, gameState));
            //systems.Add(new DebugStatsSystem(ecsWorld, gameState));
            return systems;

        }
    }
}