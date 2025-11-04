using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseLockCollection
    {
        [JsonPropertyName("locks")]
        public Dictionary<string, TestHouseLockModel> LockedDoors { get; set; } = new();
    }
}
