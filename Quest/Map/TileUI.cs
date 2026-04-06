using Adventure.Models.Map;
using Adventure.Services;

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
        /// Other active players in the same area are also rendered on the grid.
        /// </summary>
        /// <param name="tile">The tile the current player is on.</param>
        /// <param name="currentUserId">The current player's user ID (to exclude from other players). Pass 0 to skip.</param>
        public static string RenderTileGrid(TileModel tile, ulong currentUserId = 0)
        {
            // Validate and retrieve the area layout
            if (!AreaLayoutProvider.TryGetAreaLayout(tile, out var area, out var layout))
                return "<Unknown Area>";

            // Parse player coordinates
            var (playerRow, playerCol) = TilePositionParser.Parse(tile.TilePosition);

            // Find other active players in the same area
            HashSet<(int Row, int Col)>? otherPlayerPositions = null;
            if (currentUserId != 0)
            {
                var othersInArea = ActivePlayerTracker.GetPlayersInArea(tile.AreaId, excludeUserId: currentUserId);
                if (othersInArea.Count > 0)
                {
                    otherPlayerPositions = new HashSet<(int, int)>();
                    foreach (var (_, _, tilePosition) in othersInArea)
                    {
                        var (row, col) = TilePositionParser.Parse(tilePosition);
                        if (row >= 0 && col >= 0)
                            otherPlayerPositions.Add((row, col));
                    }
                }
            }

            // Render the grid
            return _renderer.RenderGrid(area!, layout, playerRow, playerCol, otherPlayerPositions);
        }
    }
}