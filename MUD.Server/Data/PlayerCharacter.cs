using System.ComponentModel.DataAnnotations;
using MUD.Core;

namespace MUD.Server.Data
{
    public class PlayerCharacter : IPlayerRecord
    {
        [Key]
        public int PlayerId { get; set; }
        public ulong AccountId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;

        // --- NEW: Implement new Interface properties ---
        public int RoomId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int CurrentHP { get; set; }

        // Extra fields (not in interface yet, but good to keep)
        public int Health { get; set; } // Can be treated as 'Base Max HP' or deprecated
        public int SpellPoints { get; set; }
        public DateTime LastLogin { get; set; }
    }
}