using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Text
{
    public class BattleTextModel
    {
        [JsonPropertyName("criticalHit")]
        public List<string> CriticalHit { get; set; } = new List<string>();

        [JsonPropertyName("hit")]
        public List<string> Hit { get; set; } = new List<string>();

        [JsonPropertyName("criticalMiss")]
        public List<string> CriticalMiss { get; set; } = new List<string>();

        [JsonPropertyName("miss")]
        public List<string> Miss { get; set; } = new List<string>();
    }
}
