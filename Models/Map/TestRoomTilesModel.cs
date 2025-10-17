using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestRoomTilesModel
    {
        [JsonPropertyName("room1")]
        public List<TileModel> Room1 { get; set; } = new();

        [JsonPropertyName("room2")]
        public List<TileModel> Room2 { get; set; } = new();
    }
}
