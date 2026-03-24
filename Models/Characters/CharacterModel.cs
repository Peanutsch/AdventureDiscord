using Adventure.Models.Attributes;
using System.Text.Json.Serialization;

namespace Adventure.Models.Characters
{
    /// <summary>
    /// Base model for all character types (Player, NPC, etc.)
    /// Contains shared properties common to all characters.
    /// </summary>
    public abstract class CharacterModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("attributes")]
        public AttributesModel Attributes { get; set; } = new();

        [JsonPropertyName("weapons")]
        public List<string> Weapons { get; set; } = new();

        [JsonPropertyName("armor")]
        public List<string> Armor { get; set; } = new();
    }
}
