using Arch.Core;

namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// An action component added to an entity to signal
    /// that it wishes to attack a target on its turn.
    /// </summary>
    public struct AttackActionComponent
    {
        public Entity Target;
    }
}