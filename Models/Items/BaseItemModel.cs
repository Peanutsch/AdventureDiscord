using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    /// <summary>
    /// Base model for all item types (Weapon, Armor, Potion, Valuable, etc.)
    /// Contains shared properties common to all items.
    /// </summary>
    public abstract class BaseItemModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }
    }
}
