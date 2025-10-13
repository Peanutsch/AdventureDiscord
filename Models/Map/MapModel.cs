using Newtonsoft.Json;
using System.Collections.Generic;

namespace Adventure.Models.Map
{
    public class MapModel
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "ERROR_MAP_ID";

        [JsonProperty("name")]
        public string MapName { get; set; } = "ERROR_MAP_NAME";

        [JsonProperty("description")]
        public string MapDescription { get; set; } = "ERROR_MAP_DESCRIPTION";

        [JsonProperty("connections")]
        public MapConnectionsModel? MapConnections { get; set; }

        [JsonProperty("pois")]
        public List<string>? MapPois { get; set; }

        [JsonProperty("items")]
        public List<string>? MapItems { get; set; }
    }

    public class MapConnectionsModel
    {
        [JsonProperty("north")]
        public string? North { get; set; }

        [JsonProperty("east")]
        public string? East { get; set; }

        [JsonProperty("south")]
        public string? South { get; set; }

        [JsonProperty("west")]
        public string? West { get; set; }
    }
}
