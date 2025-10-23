using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class MainHouseModel
    {
        [JsonPropertyName("Rooms")]
        public Dictionary<string, MainHouseRoomModel> Rooms { get; set; } = new();
        //public Dictionary<string, List<TileModel>> Rooms { get; set; } = new();
    }
}
