using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseTilesModel
    {
        [JsonPropertyName("Areas")]
        public Dictionary<string, List<TestHouseTileDetailModel>> Areas { get; set; } = new();
    }
}
