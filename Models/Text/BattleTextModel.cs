using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Text
{
    public class BattleTextModel
    {
        [JsonPropertyName("criticalHit")]
        public List<TextEntry> CriticalHit { get; set; } = new();

        [JsonPropertyName("hit")]
        public List<TextEntry> Hit { get; set; } = new();

        [JsonPropertyName("criticalMiss")]
        public List<TextEntry> CriticalMiss { get; set; } = new();

        [JsonPropertyName("miss")]
        public List<TextEntry> Miss { get; set; } = new();

        [JsonPropertyName("hpStatus")]
        public Dictionary<string, List<TextEntry>> HpStatus { get; set; } = new();
    }

    public class TextEntry
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }
}
