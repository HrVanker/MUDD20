namespace MUD.Rulesets.D20.Components
{
    /// <summary>
    /// A component that acts as a one-time event to signal that a player
    /// with a specific AccountId needs to be created in the world.
    /// </summary>
    public struct PlayerLoginRequestComponent
    {
        public ulong AccountId;
    }
}