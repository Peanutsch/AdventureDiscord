using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    public class ValuablesModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }
    }
}
