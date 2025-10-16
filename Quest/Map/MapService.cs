using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Provides utility methods for handling map-related operations such as
    /// determining valid exits and checking tile passability in the Adventure game world.
    /// </summary>
    public static class MapService
    {
        // Direction offsets (row change, column change, direction name)
        private static readonly (int dr, int dc, string dir)[] directions = new[]
        {
            (-1, 0, "North"), // Move up one row
            (1, 0, "South"),  // Move down one row
            (0, -1, "West"),  // Move left one column
            (0, 1, "East")    // Move right one column
        };

        /// <summary>
        /// Retrieves a dictionary of possible exits (direction → target tile ID)
        /// from the specified tile based on its surrounding tiles in the map.
        /// </summary>
        /// <param name="tile">The current tile from which to find exits.</param>
        /// <param name="tileLookup">A dictionary of all tiles in the map keyed by their TilePosition (e.g., "2,3").</param>
        /// <returns>
        /// A dictionary where each key is a direction (e.g., "North") and the value is the corresponding target TileId.
        /// </returns>
        public static Dictionary<string, string> GetExits(TileModel tile, Dictionary<string, TileModel> tileLookup)
        {
            LogService.DividerParts(1, "MapService.GetExits");

            var exits = new Dictionary<string, string>();

            // Ensure the tile has a valid position (e.g., "2,3")
            if (tile.TilePosition == null)
                return exits;

            // Parse the row and column from the tile's position string
            var parts = tile.TilePosition.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col))
                return exits;

            // Check each of the four directions (N, S, W, E)
            var counter = 0; // exit counter
            foreach (var (dr, dc, dir) in directions)
            {
                int newRow = row + dr;
                int newCol = col + dc;

                // Build the position string for the neighboring tile
                string newPos = $"{newRow},{newCol}";

                
                // Look up the neighboring tile and verify it's passable
                if (tileLookup.TryGetValue(newPos, out var neighborTile))
                {
                    if (IsTilePassable(neighborTile))
                    {
                        LogService.Info("[MapService.GetExits] Found exit neighbour tile");

                        exits[dir] = neighborTile.TileId;

                        counter += 1;
                    }
                }
            }

            // Log found exits for debugging
            LogService.Info($"Returning [{counter}] Exits:\n" + string.Join("", exits.Select(kv => $"{kv.Key}->{kv.Value}\n")));
            LogService.DividerParts(2, "MapService.GetExits");

            return exits;
        }

        /// <summary>
        /// Determines if a tile can be entered (walkable) based on its grid contents.
        /// </summary>
        /// <param name="tile">The tile to evaluate.</param>
        /// <returns>
        /// True if the tile is passable (contains Floor, Door, PLAYER, or START); otherwise, false.
        /// </returns>
        private static bool IsTilePassable(TileModel tile)
        {
            if (tile?.TileGrid == null)
                return false;

            // Check all cells within the tile grid
            foreach (var row in tile.TileGrid)
            {
                foreach (var cell in row)
                {
                    // Allow walking onto Floor, Door, Player, or Start positions
                    if (cell == "Floor" || cell == "Door" || cell == "PLAYER" || cell == "START")
                        return true;
                }
            }

            // Tiles like Wall, Water, etc. are impassable
            return false;
        }
    }
}
