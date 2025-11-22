using Arch.Core;
using System.Collections.Generic;

namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// A component that marks an entity as an item.
    /// It can also hold item-specific data.
    /// </summary>
    public struct ItemComponent
    {
        // We can add properties like weight, value, etc. here later.
    }

    /// <summary>
    /// A component that gives an entity an inventory to hold items.
    /// </summary>
    public struct InventoryComponent
    {
        public List<Entity> Items;
    }

    /// <summary>
    /// A component for items that can be wielded as weapons.
    /// </summary>
    public struct WeaponComponent
    {
        public int DamageDice { get; set; }
        public int DamageSides { get; set; }
    }

    /// <summary>
    /// A component that defines the "slots" an entity has for equipping items.
    /// The Entity references point to the equipped item entities.
    /// </summary>
    public struct EquipmentComponent
    {
        public Entity MainHand;
        public Entity OffHand;
        public Entity Armor;
        // We can add more slots like Head, Feet, etc. later.
    }
}