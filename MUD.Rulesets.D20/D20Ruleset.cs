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
            Console.WriteLine($"Scanning for content in module: {worldModulePath}");

            if (!Directory.Exists(worldModulePath))
            {
                Console.WriteLine($"Error: World module path not found at {worldModulePath}");
                return;
            }

            // Find all .toml files in the directory and any subdirectories
            var contentFiles = Directory.GetFiles(worldModulePath, "*.toml", SearchOption.AllDirectories);
            Console.WriteLine($"Found {contentFiles.Length} content files to load.");

            foreach (var file in contentFiles)
            {
                LoadEntityFromFile(ecsWorld, file);
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

            //systems.Add(new DebugNameSystem(ecsWorld, gameState));
            //systems.Add(new DebugStatsSystem(ecsWorld, gameState));
            return systems;

        }
    }
}