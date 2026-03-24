using Adventure.Models.Characters;
using Adventure.Models.Items;
using System.Text.Json.Serialization;

namespace Adventure.Models.NPC
{
    /// <summary>
    /// Represents an NPC (Non-Player Character) with combat and loot properties.
    /// Inherits shared character properties from CharacterModel.
    /// </summary>
    public class NpcModel : CharacterModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("challengeRate")]
        public double CR { get; set; }

        [JsonPropertyName("thumbnail_100")]
        public string ThumbHpNpc_100 { get; set; } = $"Error loading Thumbnail_100";

        [JsonPropertyName("thumbnail_50")]
        public string ThumbHpNpc_50 { get; set; } = $"Error loading Thumbnail_50";

        [JsonPropertyName("thumbnail_10")]
        public string ThumbHpNpc_10 { get; set; } = $"Error loading Thumbnail_10";

        [JsonPropertyName("thumbnail_0")]
        public string ThumbHpNpc_0 { get; set; } = $"Error loading Thumbnail_0";

        [JsonPropertyName("loot")]
        public List<LootItemModel> Loot { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();
    }
}
