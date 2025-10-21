using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class RoomData
    {
        public string[][] Layout { get; set; } = [];
        public string Description { get; set; } = string.Empty;
        public string Connections { get; set; } = string.Empty;
    }

    public class TestHouseModel
    {
        public Dictionary<string, RoomData> Rooms { get; set; } = new();
    }
}
