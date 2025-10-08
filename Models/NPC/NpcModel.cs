using Adventure.Models.Attributes;
using Adventure.Models.Items;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.NPC
{
    public class NpcModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("challengeRate")]
        public double CR { get; set; }

        [JsonPropertyName("attributes")]
        public AttributesModel Attributes { get; set; } = new();

        [JsonPropertyName("thumbnail_100")]
        public string ThumbnailNpc_100 { get; set; } = $"Error loading Thumbnail_100";

        [JsonPropertyName("thumbnail_50")]
        public string ThumbnailNpc_50 { get; set; } = $"Error loading Thumbnail_50";

        [JsonPropertyName("thumbnail_10")]
        public string ThumbnailNpc_10 { get; set; } = $"Error loading Thumbnail_10";

        [JsonPropertyName("thumbnail_0")]
        public string ThumbnailNpc_0 { get; set; } = $"Error loading Thumbnail_0";


        [JsonPropertyName("weapons")]
        public List<string> Weapons { get; set; } = new();

        [JsonPropertyName("armor")]
        public List<string> Armor { get; set; } = new();

        [JsonPropertyName("loot")]
        public List<LootItemModel> Loot { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();
    }


}
