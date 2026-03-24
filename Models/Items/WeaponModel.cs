using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    /// <summary>
    /// Represents a weapon item with range and damage properties.
    /// Inherits shared item properties from BaseItemModel.
    /// </summary>
    public class WeaponModel : BaseItemModel
    {
        [JsonPropertyName("range")]
        public double Range { get; set; }

        [JsonPropertyName("damage")]
        public DamageModel Damage { get; set; } = new();
    }

    /// <summary>
    /// Represents damage properties for weapons.
    /// </summary>
    public class DamageModel
    {
        [JsonPropertyName("diceCount")]
        public int DiceCount { get; set; }

        [JsonPropertyName("diceValue")]
        public int DiceValue { get; set; }

        [JsonPropertyName("modifier")]
        public int Modifier { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("display")]
        public string? Display { get; set; }
    }
}
