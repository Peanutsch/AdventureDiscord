using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Items
{
    public class ItemModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("points")]
        public int Points { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("effect")]
        public EffectModel Effect { get; set; } = new();
    }

    public class EffectModel
    {
        [JsonPropertyName("diceCount")]
        public int DiceCount { get; set; }

        [JsonPropertyName("diceValue")]
        public int DiceValue { get; set; }
        
        [JsonPropertyName("bonusHP")]
        public int BonusHP { get; set; }

    }
}