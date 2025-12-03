using Arch.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace MUD.Rulesets.D20.GameSystems
{
    public class EntityFactory
    {
        private readonly World _world;
        private readonly Dictionary<string, string> _templateRegistry;

        public EntityFactory(World world, Dictionary<string, string> registry)
        {
            _world = world;
            _templateRegistry = registry;
        }

        public Entity Create(string archetypeId, List<string> templates = null)
        {
            var entity = _world.Create();

            // 1. Apply Base Archetype
            ApplyTemplate(entity, archetypeId);

            // 2. Apply Templates (e.g., "vampire", "level_up_wizard")
            if (templates != null)
            {
                foreach (var tmpl in templates)
                {
                    ApplyTemplate(entity, tmpl);
                }
            }

            return entity;
        }

        public void ApplyTemplate(Entity entity, string templateId)
        {
            if (!_templateRegistry.ContainsKey(templateId))
            {
                Console.WriteLine($"Error: Template '{templateId}' not found.");
                return;
            }

            try
            {
                var model = Toml.ToModel(File.ReadAllText(_templateRegistry[templateId]));

                // --- Identity ---
                if (model.TryGetValue("name", out var n)) _world.AddOrGet<NameComponent>(entity).Name = (string)n;
                if (model.TryGetValue("description", out var d)) _world.AddOrGet<DescriptionComponent>(entity).Description = (string)d;

                // --- Stats & Vitals ---
                if (model.TryGetValue("stats", out var s) && s is TomlTable stats)
                {
                    ref var c = ref _world.AddOrGet<CoreStatsComponent>(entity);
                    // FIX: Use += to allow templates to stack bonuses on top of the base archetype
                    if (stats.ContainsKey("strength")) c.Strength += Convert.ToInt32(stats["strength"]);
                    if (stats.ContainsKey("dexterity")) c.Dexterity += Convert.ToInt32(stats["dexterity"]);
                    if (stats.ContainsKey("constitution")) c.Constitution += Convert.ToInt32(stats["constitution"]);
                    if (stats.ContainsKey("intelligence")) c.Intelligence += Convert.ToInt32(stats["intelligence"]);
                    if (stats.ContainsKey("wisdom")) c.Wisdom += Convert.ToInt32(stats["wisdom"]);
                    if (stats.ContainsKey("charisma")) c.Charisma += Convert.ToInt32(stats["charisma"]);
                }

                if (model.TryGetValue("vitals", out var v) && v is TomlTable vitals)
                {
                    ref var c = ref _world.AddOrGet<VitalsComponent>(entity);
                    if (vitals.ContainsKey("max_hp")) c.MaxHP += Convert.ToInt32(vitals["max_hp"]); // Note: += for templates!
                    c.CurrentHP = c.MaxHP;
                }

                if (model.TryGetValue("combat", out var cbt) && cbt is TomlTable combat)
                {
                    ref var c = ref _world.AddOrGet<CombatStatsComponent>(entity);
                    if (combat.ContainsKey("natural_armor")) c.NaturalArmor += Convert.ToInt32(combat["natural_armor"]);
                    if (combat.ContainsKey("base_attack_bonus")) c.BaseAttackBonus += Convert.ToInt32(combat["base_attack_bonus"]);
                }
                if (model.TryGetValue("weapon", out var w) && w is TomlTable weapon)
                {
                    ref var c = ref _world.AddOrGet<WeaponComponent>(entity);
                    if (weapon.ContainsKey("damage_dice")) c.DamageDice = Convert.ToInt32(weapon["damage_dice"]);
                    if (weapon.ContainsKey("damage_sides")) c.DamageSides = Convert.ToInt32(weapon["damage_sides"]);
                }

                if (model.TryGetValue("armor", out var a) && a is TomlTable armor)
                {
                    ref var c = ref _world.AddOrGet<ArmorComponent>(entity);
                    if (armor.ContainsKey("armor_bonus")) c.ArmorBonus = Convert.ToInt32(armor["armor_bonus"]);
                    if (armor.ContainsKey("max_dex_bonus")) c.MaxDexBonus = Convert.ToInt32(armor["max_dex_bonus"]);
                    if (armor.ContainsKey("check_penalty")) c.ArmorCheckPenalty = Convert.ToInt32(armor["check_penalty"]);
                    if (armor.ContainsKey("type")) c.ArmorType = armor["type"].ToString();
                }
                if (model.TryGetValue("progression", out var prog) && prog is TomlTable progression)
                {
                    ref var c = ref _world.AddOrGet<ExperienceComponent>(entity);
                    if (progression.ContainsKey("level")) c.Level = Convert.ToInt32(progression["level"]);
                    if (progression.ContainsKey("current_xp")) c.CurrentXP = Convert.ToInt32(progression["current_xp"]);
                }

                // Also parsing for [xp_reward] on monsters
                if (model.TryGetValue("combat", out var cbt2) && cbt2 is TomlTable combat2)
                {
                    if (combat2.ContainsKey("xp_reward"))
                    {
                        _world.AddOrGet<XpRewardComponent>(entity).Amount = Convert.ToInt32(combat2["xp_reward"]);
                    }
                }

                // --- Components Tagging ---
                if (model.ContainsKey("item")) _world.AddOrGet<ItemComponent>(entity);
                if (model.ContainsKey("inventory")) _world.AddOrGet<InventoryComponent>(entity).Items ??= new List<Entity>();

                Console.WriteLine($"Applied template '{templateId}' to entity.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying template {templateId}: {ex.Message}");
            }
        }
    }

    // Helper extension to make the code cleaner
    public static class WorldExtensions
    {
        public static ref T AddOrGet<T>(this World world, Entity entity) where T : new()
        {
            if (!world.Has<T>(entity)) world.Add(entity, new T());
            return ref world.Get<T>(entity);
        }
    }
}