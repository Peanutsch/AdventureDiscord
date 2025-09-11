using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Player
{
    public class PlayerInventoryArmorModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
