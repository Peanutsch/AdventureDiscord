using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Adventure.Models.Map
{
    public class MapModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "ERROR_MAP_ID";

        [JsonPropertyName("name")]
        public string MapName { get; set; } = "ERROR_MAP_NAME";

        [JsonPropertyName("description")]
        public string MapDescription { get; set; } = "ERROR_MAP_DESCRIPTION";

        [JsonPropertyName("connections")]
        public MapConnectionsModel? MapConnections { get; set; }

        [JsonPropertyName("pois")]
        public List<string>? MapPois { get; set; }

        [JsonPropertyName("items")]
        public List<string>? MapItems { get; set; }
    }

    public class MapConnectionsModel
    {
        [JsonPropertyName("north")]
        public string? North { get; set; }

        [JsonPropertyName("east")]
        public string? East { get; set; }

        [JsonPropertyName("south")]
        public string? South { get; set; }

        [JsonPropertyName("west")]
        public string? West { get; set; }
    }
}
