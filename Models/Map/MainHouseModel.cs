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
        [JsonPropertyName("Area")]
        public Dictionary<string, MainHouseAreaModel> Area { get; set; } = new();
    }
}
