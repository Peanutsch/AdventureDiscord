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

        [JsonPropertyName("overlays")]
        public List<string>? Overlays { get; set; }

        [JsonPropertyName("poi")]
        public List<string>? TilePOI { get; set; }

        [JsonPropertyName("items")]
        public List<string>? TileItems { get; set; }

        [JsonPropertyName("connections")]
        public List<string> Connections { get; set; } = new();

        public string TileType { get; set; } = "ERROR_TILE_TYPE";

        public string AreaId { get; set; } = "ERROR_ROOM_ID_tileModel";

        /// <summary>
        /// Berekent de richting van een connected tile ten opzichte van deze tile
        /// </summary>
        public string? TileDirectionFrom(TileModel origin)
        {
            var partsOrigin = origin.TilePosition.Split(',');
            var partsTarget = this.TilePosition.Split(',');

            int row0 = int.Parse(partsOrigin[0]);
            int col0 = int.Parse(partsOrigin[1]);
            int row1 = int.Parse(partsTarget[0]);
            int col1 = int.Parse(partsTarget[1]);

            if (row1 == row0 - 1 && col1 == col0) return "North";  // omhoog
            if (row1 == row0 + 1 && col1 == col0) return "South";  // omlaag
            if (row1 == row0 && col1 == col0 - 1) return "West";   // links
            if (row1 == row0 && col1 == col0 + 1) return "East";   // rechts

            return null; // niet aangrenzend
        }
    }
}