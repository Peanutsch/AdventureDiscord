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
        [JsonPropertyName("tiles_Room_1")]
        public List<TileModel> TilesRoom1 { get; set; } = new List<TileModel>();
    }
}
