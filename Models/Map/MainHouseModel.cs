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
        //public List<TileModel>? Room1 { get; set; }
        //public List<TileModel>? Room2 { get; set; }

        public Dictionary<string, List<TileModel>> Rooms { get; set; } = new();
    }
}
