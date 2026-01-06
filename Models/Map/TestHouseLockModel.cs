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
        [JsonPropertyName("lockType")]
        public string LockType { get; set; } = string.Empty;

        [JsonPropertyName("keyId")]
        public string KeyId { get; set; } = "ERROR_KeyID";

        [JsonPropertyName("keyhole")]
        public bool KeyHole { get; set; }

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }
    }
}
