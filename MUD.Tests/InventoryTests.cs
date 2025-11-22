using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUD.Rulesets.D20.Components;
using System.Collections.Generic;
using System.Linq;

namespace MUD.Tests
{
    [TestClass]
    public class InventoryTests
    {
        [TestMethod]
        public void CanPlaceItemInInventory()
        {
            // --- ARRANGE ---
            var world = World.Create();

            // Create a player with an inventory
            var player = world.Create(
                new InventoryComponent { Items = new List<Entity>() }
            );

            // Create a sword item
            var sword = world.Create(
                new ItemComponent()
            );

            // --- ACT ---
            // Get the player's inventory and add the sword to it.
            ref var playerInventory = ref world.Get<InventoryComponent>(player);
            playerInventory.Items.Add(sword);

            // --- ASSERT ---
            // Verify that the sword is now in the inventory list.
            Assert.AreEqual(1, playerInventory.Items.Count);
            Assert.IsTrue(playerInventory.Items.Contains(sword));
        }
    }
}