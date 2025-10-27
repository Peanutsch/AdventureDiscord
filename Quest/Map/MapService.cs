using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adventure.Quest.Map
{
    /*
      Provides helper methods for accessing and interacting with the map data.
      Handles retrieving tiles, areas, exits, and tracking player positions.
    */

    public static class MapService
    {
        /// <summary>
        /// Retrieves all loaded areas in the TestHouse.
        /// </summary>
        public static Dictionary<string, TestHouseAreaModel> Areas => TestHouseLoader.AreaLookup;

        /// <summary>
        /// Returns a dictionary of valid exits for a given tile.
        /// Supports connections in the format "areaId:TileName" (e.g., "small_pond:DOOR").
        /// </summary>
        public static Dictionary<string, string> GetExits(TileModel tile)
        {
            var exits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (tile.Connections == null || !tile.Connections.Any())
            {
                LogService.Info("[MapService.GetExits] No connections defined for this tile.");
                return exits;
            }

            foreach (var connection in tile.Connections)
            {
                var parts = connection.Split(':');
                if (parts.Length != 2)
                {
                    LogService.Error($"[MapService.GetExits] Invalid connection format: {connection}");
                    continue;
                }

                string targetAreaId = parts[0];
                string targetTileName = parts[1];

                if (!Areas.TryGetValue(targetAreaId, out var targetArea))
                {
                    LogService.Error($"[MapService.GetExits] Target area '{targetAreaId}' not found.");
                    continue;
                }

                // Find the first tile in target area that matches TileName
                var targetTile = targetArea.Tiles.FirstOrDefault(t =>
                    string.Equals(t.TileName, targetTileName, StringComparison.OrdinalIgnoreCase));

                if (targetTile != null)
                {
                    exits[targetAreaId] = targetTile.TileId;
                }
                else
                {
                    LogService.Error($"[MapService.GetExits] Tile '{targetTileName}' not found in area '{targetAreaId}'.");
                }
            }

            return exits;
        }
    }
}
    /*
    public static class MapService
    {
        // Directions and their offsets in the grid
        private static readonly (int rowOffset, int colOffset, string directionName)[] directions = new[]
        {
            (-1, 0, "North"),  // move up 1 row
            (1, 0, "South"),   // move down 1 row
            (0, -1, "West"),   // move left 1 column
            (0, 1, "East")     // move right 1 column
        };

        /// <summary>
        /// Returns a dictionary of valid exits for a given tile.
        /// Key = direction name (North, South, West, East)
        /// Value = TileId of the neighboring tile
        /// </summary>
        /// <param name="tile">The current tile</param>
        /// <param name="tileLookup">Lookup table: RoomName:TilePosition -> TileModel</param>
        /// <returns>Dictionary of exits and their destination TileIds</returns>
        public static Dictionary<string, string> GetExits(TileModel tile, Dictionary<string, TileModel> tileLookup)
        {
            LogService.DividerParts(1, "MapService.GetExits");

            var exits = new Dictionary<string, string>();

            if (tile.TilePosition == null)
                return exits;

            // --- Determine the room id that contains this tile --- 
            var roomId = tile.AreaId;

            LogService.Info($"roomId: {roomId}");

            // --- Split the tile position into row and column integers --- 
            var parts = tile.TilePosition.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col))
                return exits;

            // --- Check each possible direction ---
            foreach (var (rowOffset, colOffset, directionName) in directions)
            {
                int newRow = row + rowOffset;
                int newCol = col + colOffset;

                // --- Key volgens nieuwe lookup: roomId + tileId ---
                string neighborTileId = $"tile_{newRow}_{newCol}";
                string key = $"{roomId}:{neighborTileId}";

                if (tileLookup.TryGetValue(key, out var neighborTile))
                {
                    if (IsTilePassable(neighborTile))
                        exits[directionName] = key; // return de key zelf
                }
            }

            LogService.Info("Returning Exits: " + string.Join(", ", exits.Select(kv => $"{kv.Key}->{kv.Value}")));
            LogService.DividerParts(2, "MapService.GetExits");
            return exits;
        }

        /// <summary>
        /// Determines if the given tile is passable for the player.
        /// Only tiles containing "Floor", "Door", "PLAYER", or "START" are considered passable.
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        /// <returns>True if the tile is passable; otherwise, false.</returns>
        private static bool IsTilePassable(TileModel tile)
        {
            if (tile?.TileGrid == null)
                return false;

            foreach (var row in tile.TileGrid)
            {
                foreach (var cell in row)
                {
                    if (cell == "Floor" || cell == "Door" || cell == "PLAYER" || cell == "START")
                        return true;
                }
            }

            // All other cells (e.g., "Wall", "Water") block movement
            return false;
        }
    }
}
*/