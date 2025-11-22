using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUD.Rulesets.D20; // Use our new rules helper

namespace MUD.Tests
{
    [TestClass]
    public class RulesLogicTests
    {
        [TestMethod]
        public void GetAbilityModifier_CalculatesCorrectly()
        {
            // A score of 10 should be a +0 modifier
            Assert.AreEqual(0, D20Rules.GetAbilityModifier(10));

            // A score of 11 should also be +0 (rounded down)
            Assert.AreEqual(0, D20Rules.GetAbilityModifier(11));

            // A score of 12 should be a +1 modifier
            Assert.AreEqual(1, D20Rules.GetAbilityModifier(12));

            // A score of 18 should be a +4 modifier
            Assert.AreEqual(4, D20Rules.GetAbilityModifier(18));

            // A score of 7 should be a -2 modifier
            Assert.AreEqual(-2, D20Rules.GetAbilityModifier(7));
        }
    }
}