using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestMapModel
    {
        [JsonPropertyName("testMap1")]
        public List<TileModel>? TestMap1 { get; set; }

        [JsonPropertyName("testMap2")]
        public List<TileModel>? TestMap2 { get; set; }
    }
}
