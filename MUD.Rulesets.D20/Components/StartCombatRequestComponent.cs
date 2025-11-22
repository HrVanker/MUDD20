using Arch.Core;
using System.Collections.Generic;

namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// An event component that requests a combat encounter to begin.
    /// </summary>
    public struct StartCombatRequestComponent
    {
        // A list of all entities that will participate in the combat.
        public List<Entity> Combatants;
    }
}