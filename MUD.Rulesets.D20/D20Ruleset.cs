using Arch.Core;
using Arch.System;
using MUD.Core;
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

        // The Registry: Maps "goblin_grunt" -> "Content/Aethelgard/creatures/goblin.toml"
        private readonly Dictionary<string, string> _templateRegistry = new Dictionary<string, string>();
        private EntityFactory _entityFactory;
        public EntityFactory Factory => _entityFactory;

        private class RandomDiceRoller : IDiceRoller
        {
            private readonly Random _random;
            public RandomDiceRoller(Random random) => _random = random;
            public int Roll(int sides) => _random.Next(1, sides + 1);
        }

        private void RegisterTemplate(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var model = Toml.ToModel(content);

                if (model.TryGetValue("id", out var idObj))
                {
                    string id = idObj.ToString();
                    if (_templateRegistry.ContainsKey(id))
                    {
                        Console.WriteLine($"Warning: Duplicate template ID '{id}' in {Path.GetFileName(filePath)}. Overwriting.");
                    }
                    _templateRegistry[id] = filePath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register template {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        public void LoadContent(World ecsWorld, string worldModulePath)
        {
            Console.WriteLine($"Loading world module from: {worldModulePath}");
            string manifestPath = Path.Combine(worldModulePath, "world.toml");

            if (!File.Exists(manifestPath))
            {
                Console.WriteLine($"CRITICAL: World manifest not found at {manifestPath}");
                return;
            }

            try
            {
                var manifest = Toml.ToModel(File.ReadAllText(manifestPath));

                // 1. SCAN CONTENT DIRECTORIES
                if (manifest.TryGetValue("content_directories", out var contentDirsObj) && contentDirsObj is TomlArray contentDirs)
                {
                    foreach (var dirName in contentDirs)
                    {
                        string dirPath = Path.Combine(worldModulePath, dirName.ToString());
                        if (Directory.Exists(dirPath))
                        {
                            string[] files = Directory.GetFiles(dirPath, "*.toml", SearchOption.AllDirectories);
                            Console.WriteLine($"Scanning {dirName}: Found {files.Length} templates.");

                            foreach (string file in files)
                            {
                                RegisterTemplate(file);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Content directory not found: {dirPath}");
                        }
                    }
                }

                _entityFactory = new EntityFactory(ecsWorld, _templateRegistry);

                // 2. LOAD AREAS
                if (manifest.TryGetValue("area_directories", out var areaDirsObj) && areaDirsObj is TomlArray areaDirs)
                {
                    foreach (var dirName in areaDirs)
                    {
                        string dirPath = Path.Combine(worldModulePath, dirName.ToString());
                        if (Directory.Exists(dirPath))
                        {
                            string[] files = Directory.GetFiles(dirPath, "*.toml", SearchOption.AllDirectories);
                            Console.WriteLine($"Loading Areas from {dirName}: Found {files.Length} files.");

                            foreach (string file in files)
                            {
                                LoadAreaFromFile(ecsWorld, file);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading world manifest: {ex.Message}");
            }
        }

        // FIX: Added arguments (roomId, x, y) to the signature so they can be used inside.
        private void LoadEntityFromFile(World ecsWorld, string filePath, int roomId, int x, int y)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);
                var tomlModel = Toml.ToModel(fileContent);
                var entity = ecsWorld.Create();

                // Now using the passed arguments
                ecsWorld.Add(entity, new LocationComponent { RoomId = roomId, X = x, Y = y });

                int componentsAdded = 0;

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

                if (tomlModel.TryGetValue("weapon", out var weaponValue) && weaponValue is TomlTable weaponTable)
                {
                    ecsWorld.Add(entity, new WeaponComponent
                    {
                        DamageDice = Convert.ToInt32(weaponTable["damage_dice"]),
                        DamageSides = Convert.ToInt32(weaponTable["damage_sides"])
                    });
                    componentsAdded++;
                }

                if (tomlModel.TryGetValue("armor", out var armorValue) && armorValue is TomlTable armorTable)
                {
                    ecsWorld.Add(entity, new ArmorComponent
                    {
                        ArmorBonus = Convert.ToInt32(armorTable["armor_bonus"]),
                        MaxDexBonus = Convert.ToInt32(armorTable["max_dex_bonus"]),
                        ArmorCheckPenalty = Convert.ToInt32(armorTable["check_penalty"]),
                        ArmorType = armorTable["type"].ToString()
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

                if (tomlModel.TryGetValue("combat", out var combatValue) && combatValue is TomlTable combatTable)
                {
                    ecsWorld.Add(entity, new CombatStatsComponent
                    {
                        NaturalArmor = Convert.ToInt32(combatTable["armor_class"]),
                        BaseAttackBonus = Convert.ToInt32(combatTable["base_attack_bonus"])
                    });
                    componentsAdded++;
                }

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

                if (componentsAdded > 0)
                {
                    Console.WriteLine($"Successfully created entity from {Path.GetFileName(filePath)}.");
                }
                else
                {
                    ecsWorld.Destroy(entity);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading entity from {filePath}: {ex.Message}");
            }
        }

        private void SpawnEntity(World ecsWorld, string templateId, int roomId, int x, int y)
        {
            // Parsing "goblin_grunt" OR "goblin_grunt+vampire"
            var templates = new List<string>();
            string archetype = templateId;

            if (templateId.Contains("+"))
            {
                var parts = templateId.Split('+');
                archetype = parts[0];
                for (int i = 1; i < parts.Length; i++) templates.Add(parts[i]);
            }

            // Use the factory to create the entity with all templates applied
            var entity = _entityFactory.Create(archetype, templates);

            // Set Location (The factory doesn't know about rooms/coordinates)
            ecsWorld.Add(entity, new LocationComponent { RoomId = roomId, X = x, Y = y });
        }

        private void LoadAreaFromFile(World ecsWorld, string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);
                var tomlModel = Toml.ToModel(fileContent);

                // FIX: Check for 'TomlTableArray' instead of 'TomlArray'
                // [[rooms]] in TOML creates a TomlTableArray, not a generic TomlArray.
                if (tomlModel.TryGetValue("rooms", out var roomsObj) && roomsObj is TomlTableArray roomsArray)
                {
                    Console.WriteLine($"  - Parsing {roomsArray.Count} rooms from {Path.GetFileName(filePath)}...");

                    foreach (TomlTable roomTable in roomsArray)
                    {
                        var entity = ecsWorld.Create();
                        int roomId = Convert.ToInt32(roomTable["id"]);

                        var roomComp = new RoomComponent
                        {
                            Title = roomTable["title"].ToString(),
                            Description = roomTable["description"].ToString(),
                            AreaId = roomId,
                            Exits = new Dictionary<string, int>(),
                            Width = roomTable.ContainsKey("width") ? Convert.ToInt32(roomTable["width"]) : 10,
                            Height = roomTable.ContainsKey("height") ? Convert.ToInt32(roomTable["height"]) : 10
                        };

                        // Parse Exits
                        if (roomTable.TryGetValue("exits", out var exitsObj) && exitsObj is TomlTable exitsTable)
                        {
                            foreach (var exit in exitsTable)
                            {
                                roomComp.Exits[exit.Key] = Convert.ToInt32(exit.Value);
                            }
                        }

                        // --- SPAWN LOGIC ---
                        // Note: 'spawns' is usually a nested [[rooms.spawns]] which is ALSO a TomlTableArray
                        if (roomTable.TryGetValue("spawns", out var spawnsObj) && spawnsObj is TomlTableArray spawnsArray)
                        {
                            foreach (TomlTable spawn in spawnsArray)
                            {
                                string templateId = spawn["template"].ToString();
                                int x = spawn.ContainsKey("x") ? Convert.ToInt32(spawn["x"]) : 0;
                                int y = spawn.ContainsKey("y") ? Convert.ToInt32(spawn["y"]) : 0;

                                SpawnEntity(ecsWorld, templateId, roomId, x, y);
                            }
                        }

                        ecsWorld.Add(entity, roomComp);
                        ecsWorld.Add(entity, new LocationComponent { RoomId = roomId });
                    }
                    Console.WriteLine($"Successfully loaded area: {Path.GetFileName(filePath)}");
                }
                else
                {
                    Console.WriteLine($"Warning: '{filePath}' was loaded but contained no '[[rooms]]' block.");
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

            systems.Add(new MovementSystem(ecsWorld));
            systems.Add(new SkillCheckSystem(ecsWorld, gameState, diceRoller));
            systems.Add(new InitiativeSystem(ecsWorld, gameState));
            systems.Add(new CombatSystem(ecsWorld, gameState, diceRoller));
            systems.Add(new RecoverySystem(ecsWorld));
            return systems;
        }
    }
}