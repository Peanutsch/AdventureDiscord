using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Adventure.Models.Map
{
    public class TileModel
    {
        [JsonPropertyName("id")]
        public string TileId { get; set; } = "ERROR_TILE_ID";

        [JsonPropertyName("name")]
        public string TileName { get; set; } = "ERROR_TILE_NAME";

        [JsonPropertyName("description")]
        public string TileDescription { get; set; } = "ERROR_TILE_DESCRIPTION";

        [JsonPropertyName("position")]
        public string TilePosition { get; set; } = "ERROR_TILE_POSITION";

        [JsonPropertyName("grid")]
        public List<List<string>> TileGrid { get; set; } = new();

        [JsonPropertyName("text")]
        public string TileText { get; set; } = "ERROR_TILE_TEXT";

        [JsonPropertyName("overlays")]
        public List<string>? Overlays { get; set; }

        [JsonPropertyName("pois")]
        public List<string>? TilePois { get; set; }

        [JsonPropertyName("items")]
        public List<string>? TileItems { get; set; }
    }
}
