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
        public string Id { get; set; } = "ERROR_ID";

        [JsonPropertyName("base")]
        public string Base { get; set; } = "ERROR_BASE";

        [JsonPropertyName("overlay")]
        public string Overlay { get; set; } = "ERROR_OVERLAY";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "ERROR_TEXT";

        [JsonPropertyName("pois")]
        public List<string> Pois { get; set; } = new();

        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new();

        [JsonPropertyName("lockId")]
        public string TileLockId { get; set; } = "ERROR_LOCK_ID";

        [JsonPropertyName("lockSwitch")]
        public bool TileLockSwitch { get; set; }

        [JsonPropertyName("connections")]
        public List<string>? Connections { get; set; }
    }
}
