using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    /// <summary>
    /// Represents a general item with points, value, and effects.
    /// Inherits shared item properties from BaseItemModel.
    /// </summary>
    public class ItemModel : BaseItemModel
    {
        [JsonPropertyName("points")]
        public int Points { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("effect")]
        public EffectItems Effect { get; set; } = new();
    }

    /// <summary>
    /// Represents the effects an item can have when used.
    /// </summary>
    public class EffectItems
    {
        [JsonPropertyName("diceCount")]
        public int DiceCount { get; set; }

        [JsonPropertyName("diceValue")]
        public int DiceValue { get; set; }

        [JsonPropertyName("bonusHP")]
        public int BonusHP { get; set; }
    }
}