using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseTileDetailModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("overlays")]
        public List<string> Overlays { get; set; } = new();

        [JsonPropertyName("pois")]
        public List<string> Pois { get; set; } = new();

        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new();

        [JsonPropertyName("connections")]
        public List<string>? Connections { get; set; }
    }
}
