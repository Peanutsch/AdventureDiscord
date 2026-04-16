using Adventure.Models.Characters;
using Adventure.Models.Items;
using System.Text.Json.Serialization;

namespace Adventure.Models.Player
{
    /// <summary>
    /// Represents a Player character with inventory and progression properties.
    /// Inherits shared character properties from CharacterModel.
    /// </summary>
    public class PlayerModel : CharacterModel
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("xp")]
        public int XP { get; set; }

        [JsonPropertyName("hitpoints")]
        public int Hitpoints { get; set; }

        [JsonPropertyName("maxCarry")]
        public double MaxCarry { get; set; }

        [JsonPropertyName("savepoint")]
        public string Savepoint { get; set; } = "ERROR_LAST_SAVEPOINT";

        [JsonPropertyName("weapons")]
        public new List<PlayerInventoryWeaponsModel> Weapons { get; set; } = new();

        [JsonPropertyName("armor")]
        public new List<PlayerInventoryArmorModel> Armor { get; set; } = new();

        [JsonPropertyName("items")]
        public List<PlayerInventoryItemModel> Items { get; set; } = new();

        [JsonPropertyName("loot")]
        public List<PlayerInventoryItemModel> Loot { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();

        // Tracking Player's current state (e.g., Idle, InCombat, inAdventure)
        [JsonPropertyName("currentState")]
        public PlayerState CurrentState { get; set; } = PlayerState.Idle;

        [JsonPropertyName("lastActivityTime")]
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("lastSessionResetTime")]
        public DateTime? LastSessionResetTime { get; set; } = null;

        [JsonIgnore]
        public string? Step { get; set; }
    }
}

