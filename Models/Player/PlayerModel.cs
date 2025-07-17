using Adventure.Models.Attributes;
using Adventure.Models.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Adventure.Models.Player
{
    public class PlayerModel
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("hitpoints")]
        public int Hitpoints { get; set; }

        [JsonPropertyName("maxCarry")]
        public double MaxCarry { get; set; }

        [JsonPropertyName("attributes")]
        public AttributesModel Attributes { get; set; } = new();

        [JsonPropertyName("weapons")]
        public List<string> Weapons { get; set; } = new();

        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new();

        [JsonPropertyName("loot")]
        public List<ItemModel> Loot { get; set; } = new();

        [JsonIgnore]
        public string? Step { get; set; }

    }
}

