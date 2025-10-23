using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class MainHouseRoomModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "ERROR_ROOM_ID_mainHouseRoomModel";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "ERROR_ROOM_NAME";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "ERROR_ROOM_DESCRIPTION";

        [JsonPropertyName("tiles")]
        public List<TileModel> Tiles { get; set; } = new();

        // Genestede kamers (optioneel)
        [JsonPropertyName("subrooms")]
        public Dictionary<string, MainHouseRoomModel>? SubRooms { get; set; }
    }
}
