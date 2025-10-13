namespace MUD.Core
{
    // A simple interface that defines a player record.
    // This prevents the Core project from needing to know about the full
    // PlayerCharacter class from the Server project.
    public interface IPlayerRecord
    {
        ulong AccountId { get; }
        string CharacterName { get; }
        string Race { get; }
        string Class { get; }
    }

    // This is the contract. It says that any database service must have a
    // method to get a player record.
    public interface IDatabaseService
    {
        IPlayerRecord GetPlayerRecord(ulong accountId);
    }
}