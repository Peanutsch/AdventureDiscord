using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Items
{
    public class ArmorModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("armor_class")]
        public int ArmorClass { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "light"; // light, medium, heavy
    }

    public class ArmorContainer 
    {
        [JsonPropertyName("crafted_armor")]
        public List<ArmorModel>? CraftedArmor { get; set; }

        [JsonPropertyName("natural_armor")]
        public List<ArmorModel>? NaturalArmor { get; set; }
    }
}
