using Arch.Core;
using Arch.System;
using MUD.Core; // <-- FIX: Added missing using directive for IDiceRoller
using MUD.Rulesets.D20.Components;
using MUD.Rulesets.D20.GameSystems;
using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace MUD.Rulesets.D20
{
    public class D20Ruleset : IRuleset
    {
        public string Name => "D20 Ruleset";
        private readonly Random _random = new Random();

        // A simple implementation of the dice roller for the live game.
        private class RandomDiceRoller : IDiceRoller
        {
            private readonly Random _random;
            public RandomDiceRoller(Random random) => _random = random;
            public int Roll(int sides) => _random.Next(1, sides + 1);
        }

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
                string manifestContent = File.ReadAllText(manifestPath);
                var manifest = Toml.ToModel(manifestContent);

                // Load Creatures
                if (manifest.TryGetValue("creatures", out var creatureFilesObj) && creatureFilesObj is TomlArray creatureFilesArray)
                {
                    Console.WriteLine($"Found {creatureFilesArray.Count} creature(s) to load from manifest.");
                    foreach (var creatureFile in creatureFilesArray)
                    {
                        if (creatureFile == null) continue;
                        string fullPath = Path.Combine(worldModulePath, creatureFile.ToString());
                        LoadEntityFromFile(ecsWorld, fullPath);
                    }
                }

                // --- NEW CODE ---
                // Load Items
                if (manifest.TryGetValue("items", out var itemFilesObj) && itemFilesObj is TomlArray itemFilesArray)
                {
                    Console.WriteLine($"Found {itemFilesArray.Count} item(s) to load from manifest.");
                    foreach (var itemFile in itemFilesArray)
                    {
                        if (itemFile == null) continue;
                        string fullPath = Path.Combine(worldModulePath, itemFile.ToString());
                        LoadEntityFromFile(ecsWorld, fullPath);
                    }
                }

                if (manifest.TryGetValue("areas", out var areaFilesObj) && areaFilesObj is TomlArray areaFilesArray)
                {
                    Console.WriteLine($"Found {areaFilesArray.Count} area(s) to load from manifest.");
                    foreach (var areaFile in areaFilesArray)
                    {
                        if (areaFile == null) continue;
                        string fullPath = Path.Combine(worldModulePath, areaFile.ToString());
                        LoadAreaFromFile(ecsWorld, fullPath);
                    }
                }
                // --- END NEW CODE ---
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
                var entity = ecsWorld.Create();
                ecsWorld.Add(entity, new LocationComponent { RoomId = 0, X = 0, Y = 0 });
                int componentsAdded = 0;

                // Standard components like Name and Description
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

                // --- NEW ITEM PARSING LOGIC ---
                // If it has an [item] table, it's an item.
                if (tomlModel.ContainsKey("item"))
                {
                    ecsWorld.Add(entity, new ItemComponent());
                    componentsAdded++;
                }

                if (tomlModel.ContainsKey("inventory"))
                {
                    ecsWorld.Add(entity, new InventoryComponent { Items = new List<Entity>() });
                    componentsAdded++;
                }

                // If it has a [weapon] table, add weapon data.
                if (tomlModel.TryGetValue("weapon", out var weaponValue) && weaponValue is TomlTable weaponTable)
                {
                    ecsWorld.Add(entity, new WeaponComponent
                    {
                        DamageDice = Convert.ToInt32(weaponTable["damage_dice"]),
                        DamageSides = Convert.ToInt32(weaponTable["damage_sides"])
                    });
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

        private void LoadAreaFromFile(World ecsWorld, string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);
                var tomlModel = Toml.ToModel(fileContent);

                if (tomlModel.TryGetValue("rooms", out var roomsObj) && roomsObj is TomlArray roomsArray)
                {
                    foreach (TomlTable roomTable in roomsArray)
                    {
                        var entity = ecsWorld.Create();

                        var roomComp = new RoomComponent
                        {
                            Title = roomTable["title"].ToString(),
                            Description = roomTable["description"].ToString(),
                            AreaId = Convert.ToInt32(roomTable["id"]),
                            Exits = new Dictionary<string, int>(),

                            // --- NEW: Parse Dimensions with defaults ---
                            // If "width" is missing, default to 10
                            Width = roomTable.ContainsKey("width") ? Convert.ToInt32(roomTable["width"]) : 10,
                            // If "height" is missing, default to 10
                            Height = roomTable.ContainsKey("height") ? Convert.ToInt32(roomTable["height"]) : 10
                        };

                        if (roomTable.TryGetValue("exits", out var exitsObj) && exitsObj is TomlTable exitsTable)
                        {
                            foreach (var exit in exitsTable)
                            {
                                roomComp.Exits[exit.Key] = Convert.ToInt32(exit.Value);
                            }
                        }

                        ecsWorld.Add(entity, roomComp);
                        ecsWorld.Add(entity, new LocationComponent { RoomId = roomComp.AreaId });
                    }
                    Console.WriteLine($"Loaded rooms from {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading area {filePath}: {ex.Message}");
            }
        }

        public Group<GameTime> RegisterSystems(World ecsWorld, GameState gameState)
        {
            var diceRoller = new RandomDiceRoller(_random);
            Console.WriteLine("D20 Ruleset is registering systems...");
            var systems = new Group<GameTime>("D20GameSystems");

            //systems.Add(new CharacterSheetSystem(ecsWorld, gameState));
            systems.Add(new MovementSystem(ecsWorld));
            systems.Add(new SkillCheckSystem(ecsWorld, gameState, diceRoller));
            systems.Add(new InitiativeSystem(ecsWorld, gameState));
            systems.Add(new CombatSystem(ecsWorld, gameState, diceRoller));

            return systems;
        }
    }
}