using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class MapModel
    {
        [JsonProperty("id")]
        public string MapId { get; set; } = "ERROR MAP ID";

        [JsonProperty("name")]
        public string MapName { get; set; } = "ERROR MAP NAME";

        [JsonProperty("description")]
        public string MapDescription { get; set; } = "ERROR MAP DESCRIPTION";

        public MapConnectionsModel? MapConnections { get; set; }
        [JsonProperty("pois")]
        public List<MapPoisModel>? MapPois { get; set; }

        [JsonProperty("items")]
        public List<MapItemsModel>? MapItems { get; set; }
    }

    public class MapConnectionsModel
    {
        [JsonProperty("north")]
        public string North { get; set; } = "ERROR MAP NORTH";

        [JsonProperty("east")]
        public string East { get; set; } = "ERROR MAP EAST";

        [JsonProperty("south")]
        public string South { get; set; } = "ERROR MAP SOUTH";

        [JsonProperty("west")]
        public string West { get; set; } = "ERROR MAP WEST";
    }

    public class MapPoisModel
    {
        [JsonProperty("pois")]
        public List<string>? MapPois { get; set; }
    }

    public class MapItemsModel
    {
        [JsonProperty("items")]
        public List<string>? MapItems { get; set; }
    }
}
