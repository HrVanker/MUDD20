using System.ComponentModel.DataAnnotations;
using MUD.Core;

namespace MUD.Server.Data
{
    public class PlayerCharacter : IPlayerRecord
    {
        [Key] // This marks PlayerId as the primary key
        public int PlayerId { get; set; }

        // This will eventually link to a Discord account ID, for example.
        public ulong AccountId { get; set; }

        public string CharacterName { get; set; } = string.Empty;

        public string Race { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;

        public int Health { get; set; }
        public int Mana { get; set; }

        public DateTime LastLogin { get; set; }
    }
}