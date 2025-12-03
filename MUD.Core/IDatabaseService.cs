namespace MUD.Core
{
    public interface IPlayerRecord
    {
        ulong AccountId { get; }
        string CharacterName { get; }
        string Race { get; }
        string Class { get; }
        int RoomId { get; }
        int X { get; }
        int Y { get; }
        int CurrentHP { get; }
    }

    public interface IDatabaseService
    {
        IPlayerRecord? GetPlayerRecord(ulong accountId);

        // --- NEW: Save Method ---
        void SavePlayerState(ulong accountId, int roomId, int x, int y, int currentHp);
    }
}