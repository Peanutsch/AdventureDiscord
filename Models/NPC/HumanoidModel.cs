using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.NPC
{
    public class HumanoidModel
    {
        [JsonPropertyName("humanoids")]
        public List<NpcModel>? Humanoids { get; set; }

        [JsonPropertyName("undead")]
        public List<NpcModel>? Undead { get; set; }

        [JsonPropertyName("human")]
        public List<NpcModel>? Human { get; set; }

        [JsonPropertyName("elf")]
        public List<NpcModel>? Elf { get; set; }

        [JsonPropertyName("dwarf")]
        public List<NpcModel>? Dwarf { get; set; }
    }
}
