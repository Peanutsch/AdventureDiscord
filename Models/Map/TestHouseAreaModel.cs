using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseAreaModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "ERROR_AREA_ID";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "ERROR_AREA_NAME";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "ERROR_AREA_DESCRIPTION";

        [JsonPropertyName("connections")]
        public string Connections { get; set; } = string.Empty;
        
        [JsonPropertyName("layout")]
        public List<List<string>> Layout { get; set; } = new();

        [JsonIgnore]
        public List<TileModel> Tiles { get; set; } = new();
    }
}
