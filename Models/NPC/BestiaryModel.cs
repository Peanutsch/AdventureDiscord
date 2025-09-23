using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.NPC
{
    public class BestiaryModel
    {
        [JsonPropertyName("mammals")]
        public List<NpcModel>? Mammals { get; set; }

        [JsonPropertyName("birds")]
        public List<NpcModel>? Birds { get; set; }

        [JsonPropertyName("reptiles")]
        public List<NpcModel>? Reptiles { get; set; }

        [JsonPropertyName("magicalBeasts")]
        public List<NpcModel>? MagicalBeasts { get; set; }
    }
}
