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

        [JsonPropertyName("position")]
        public string TilePosition { get; set; } = "ERROR_TILE_POSITION";

        [JsonPropertyName("grid")]
        public List<List<string>> TileGrid { get; set; } = new();

        [JsonPropertyName("text")]
        public string TileText { get; set; } = "ERROR_TILE_TEXT";

        [JsonPropertyName("poi")]
        public List<string>? TilePOI { get; set; }

        [JsonPropertyName("items")]
        public List<string>? TileItems { get; set; }

        [JsonPropertyName("connections")]
        public List<string> Connections { get; set; } = new();

        [JsonPropertyName("lockId")]
        public string? LockId { get; set; }

        public TestHouseLockModel? LockState { get; set; } = new();

        public string TileBase { get; set; } = "ERROR_TILE_BASE";

        public string TileOverlay { get; set; } = "ERROR_TILE_OVERLAY";

        public string TileType { get; set; } = "ERROR_TILE_TYPE";

        public string AreaId { get; set; } = "ERROR_ROOM_ID_tileModel";
    }
}