using Adventure.Models.Map;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Facade for tile UI operations. Orchestrates components responsible for 
    /// rendering tile grids and parsing tile positions.
    /// </summary>
    public static class TileUI
    {
        private static readonly AreaLayoutProvider _areaLayoutProvider = new();
        private static readonly TilePositionParser _positionParser = new();
        private static readonly TileIconProvider _iconProvider = new();
        private static readonly GridRenderer _renderer = new(_iconProvider);

        /// <summary>
        /// Renders a visual grid for the current area with the player icon placed at the correct position.
        /// </summary>
        public static string RenderTileGrid(TileModel tile)
        {
            // Validate and retrieve the area layout
            if (!_areaLayoutProvider.TryGetAreaLayout(tile, out var area, out var layout))
                return "<Unknown Area>";

            // Parse player coordinates
            var (playerRow, playerCol) = _positionParser.Parse(tile.TilePosition);

            // Render the grid
            return _renderer.RenderGrid(area!, layout, playerRow, playerCol);
        }

        /// <summary>
        /// Converts a TilePosition string "row,col" to row/col integers.
        /// </summary>
        public static (int row, int col) ParseTilePosition(string tilePos)
        {
            return _positionParser.Parse(tilePos);
        }

        /// <summary>
        /// Gets the emoji map for tile types (for backward compatibility if needed externally).
        /// </summary>
        public static string GetTileEmoji(string key)
        {
            return _iconProvider.GetEmoji(key);
        }
    }
}