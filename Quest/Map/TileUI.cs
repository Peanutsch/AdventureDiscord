using Adventure.Models.Map;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Facade for tile UI operations. Orchestrates components responsible for 
    /// rendering tile grids and parsing tile positions.
    /// </summary>
    public static class TileUI
    {
        private static readonly TileIconProvider _iconProvider = new();
        private static readonly GridRenderer _renderer = new(_iconProvider);

        /// <summary>
        /// Renders a visual grid for the current area with the player icon placed at the correct position.
        /// </summary>
        public static string RenderTileGrid(TileModel tile)
        {
            // Validate and retrieve the area layout
            if (!AreaLayoutProvider.TryGetAreaLayout(tile, out var area, out var layout))
                return "<Unknown Area>";

            // Parse player coordinates
            var (playerRow, playerCol) = TilePositionParser.Parse(tile.TilePosition);

            // Render the grid
            return _renderer.RenderGrid(area!, layout, playerRow, playerCol);
        }
    }
}