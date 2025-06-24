using Adventure.Models.Attributes;
using Adventure.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Creatures
{
    public class CreaturesModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("hitpoints")]
        public int Hitpoints { get; set; }

        [JsonPropertyName("attributes")]
        public AttributesModel Attributes { get; set; } = new();

        [JsonPropertyName("weapons")]
        public List<string> Weapons { get; set; } = new();

        [JsonPropertyName("armor")]
        public List<string> Armor { get; set; } = new();

        [JsonPropertyName("loot")]
        public List<LootItemModel> Loot { get; set; } = new();
    }
}
