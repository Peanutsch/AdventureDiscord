using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Items
{
    public class WeaponModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("range")]
        public required Dictionary<string, int> Range { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("damage")]
        public DamageModel Damage { get; set; } = new();

    }

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
