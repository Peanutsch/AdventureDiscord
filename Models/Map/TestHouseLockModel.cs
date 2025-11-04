using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class TestHouseLockModel
    {
        [JsonPropertyName("lock_type")]
        public string LockType { get; set; } = "---";

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }
    }
}
