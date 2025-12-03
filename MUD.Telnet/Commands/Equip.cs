using Arch.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Linq;
using System.Threading.Tasks;

public class EquipCommand : ICommand
{
    public string Name => "equip"; // Ensure this property exists if your interface requires it

    public async Task ExecuteAsync(TelnetSession session, World world, string[] args)
    {
        if (!session.PlayerEntity.HasValue) return;

        if (args.Length == 0)
        {
            await session.WriteLineAsync("Equip what?");
            return;
        }

        string itemName = args[0];
        var inventory = world.Get<InventoryComponent>(session.PlayerEntity.Value);
        Entity itemToEquip = Entity.Null;

        // Find the item
        foreach (var itemEntity in inventory.Items)
        {
            var name = world.Get<NameComponent>(itemEntity).Name;
            if (name.Contains(itemName, StringComparison.OrdinalIgnoreCase))
            {
                itemToEquip = itemEntity;
                break;
            }
        }

        if (itemToEquip == Entity.Null)
        {
            await session.WriteLineAsync("You aren't carrying that.");
            return;
        }

        var equipment = world.Get<EquipmentComponent>(session.PlayerEntity.Value);
        bool equipped = false;

        // 1. Handle Weapons
        if (world.Has<WeaponComponent>(itemToEquip))
        {
            if (world.IsAlive(equipment.MainHand)) inventory.Items.Add(equipment.MainHand);
            equipment.MainHand = itemToEquip;
            inventory.Items.Remove(itemToEquip);

            await session.WriteLineAsync($"You wield the {itemName}.");
            world.Set(session.PlayerEntity.Value, equipment);
        }
        // --- ADD THIS BLOCK ---
        else if (world.Has<ArmorComponent>(itemToEquip))
        {
            var armorComp = world.Get<ArmorComponent>(itemToEquip);

            if (armorComp.ArmorType == "Shield")
            {
                if (world.IsAlive(equipment.OffHand)) inventory.Items.Add(equipment.OffHand);
                equipment.OffHand = itemToEquip;
                await session.WriteLineAsync($"You strap the {itemName} to your arm.");
            }
            else
            {
                if (world.IsAlive(equipment.Armor)) inventory.Items.Add(equipment.Armor);
                equipment.Armor = itemToEquip;
                await session.WriteLineAsync($"You wear the {itemName}.");
            }

            inventory.Items.Remove(itemToEquip);
            world.Set(session.PlayerEntity.Value, equipment);
        }
        // ----------------------
        else
        {
            await session.WriteLineAsync("You can't equip that.");
        }

        if (equipped)
        {
            inventory.Items.Remove(itemToEquip);
            world.Set(session.PlayerEntity.Value, equipment); // Save equipment slots
            // Note: Inventory is a reference type (List), so we don't strictly need to Set() it back if we just modified the list, 
            // but if InventoryComponent was a struct with immutable fields, we would. 
            // Since it's a struct holding a List object, the List reference is the same.
        }
    }
}