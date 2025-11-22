using Arch.Core;
using MUD.Rulesets.D20.Components;
using System;
using System.Linq;
using System.Threading.Tasks;

public class EquipCommand : ICommand
{
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

        // Find the item in the player's inventory.
        foreach (var itemEntity in inventory.Items)
        {
            var name = world.Get<NameComponent>(itemEntity).Name;
            if (name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
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

        // Check what kind of item it is and equip it to the correct slot.
        if (world.Has<WeaponComponent>(itemToEquip))
        {
            var equipment = world.Get<EquipmentComponent>(session.PlayerEntity.Value);

            // For now, we just equip to the main hand.
            // A real system would check if the slot is free, handle two-handed weapons, etc.
            equipment.MainHand = itemToEquip;
            inventory.Items.Remove(itemToEquip); // Remove from inventory

            await session.WriteLineAsync($"You wield the {itemName}.");
        }
        else
        {
            await session.WriteLineAsync("You can't equip that.");
        }
    }
}