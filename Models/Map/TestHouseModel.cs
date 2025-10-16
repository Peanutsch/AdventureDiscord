using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseModel
    {
        [JsonPropertyName("rooms")]
        public Dictionary<string, RoomData> Rooms { get; set; } = new();

    }

    public class RoomData
    {
        public List<List<string>> Layout { get; set; } = new();
        public string Description { get; set; } = "ERROR_DESCRIPTION_ROOMDATA";
    }
}
