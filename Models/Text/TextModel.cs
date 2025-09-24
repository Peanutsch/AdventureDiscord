using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Text
{
    public class TextModel
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class TextContainer
    {
        [JsonPropertyName("criticalHit")]
        public List<TextModel>? CriticalHit { get; set; }

        [JsonPropertyName("hit")]
        public List<TextModel>? Hit { get; set; }

        [JsonPropertyName("criticalMiss")]
        public List<TextModel>? CriticalMiss { get; set; }

        [JsonPropertyName("miss")]
        public List<TextModel>? Miss { get; set; }
    }
}
