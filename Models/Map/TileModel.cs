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

        [JsonPropertyName("text")]
        public string TileText { get; set; } = "ERROR_TILE_TEXT";

        [JsonPropertyName("exits")]
        public TileExitsModel? TileExits { get; set; }

        [JsonPropertyName("pois")]
        public List<string>? TilePois { get; set; }

        [JsonPropertyName("items")]
        public List<string>? TileItems { get; set; }
    }

    public class TileExitsModel
    {
        [JsonPropertyName("north")]
        public string? North { get; set; }

        [JsonPropertyName("east")]
        public string? East { get; set; }

        [JsonPropertyName("south")]
        public string? South { get; set; }

        [JsonPropertyName("west")]
        public string? West { get; set; }
    }
}
