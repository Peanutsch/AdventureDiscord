using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Map
{
    public static class MapService
    {
        // Richtingen en hun verschuivingen
        private static readonly (int dr, int dc, string dir)[] directions = new[]
        {
            (-1, 0, "North"),
            (1, 0, "South"),
            (0, -1, "West"),
            (0, 1, "East")
        };

        public static Dictionary<string, string> GetExits(TileModel tile, Dictionary<string, TileModel> tileLookup)
        {
            LogService.DividerParts(1, "MapService.GetExits");

            var exits = new Dictionary<string, string>();

            if (tile.TilePosition == null)
                return exits;

            // Bepaal de roomName van deze tile via TileLookup
            var roomEntry = MainHouseLoader.Rooms.FirstOrDefault(r => r.Value.Contains(tile));
            string roomName = roomEntry.Key ?? "UnknownRoom";

            var parts = tile.TilePosition.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col))
                return exits;

            foreach (var (dr, dc, dir) in directions)
            {
                int newRow = row + dr;
                int newCol = col + dc;

                string newPos = $"{roomName}:{newRow},{newCol}"; // volledige key

                if (tileLookup.TryGetValue(newPos, out var neighborTile))
                {
                    if (IsTilePassable(neighborTile))
                        exits[dir] = neighborTile.TileId;
                }
            }

            LogService.Info("Returning Exits: " + string.Join(", ", exits.Select(kv => $"{kv.Key}->{kv.Value}")));
            LogService.DividerParts(2, "MapService.GetExits");
            return exits;
        }

        // Check of een tile betreedbaar is (Floor, Door, START of PLAYER)
        private static bool IsTilePassable(TileModel tile)
        {
            if (tile?.TileGrid == null) return false;

            foreach (var row in tile.TileGrid)
            {
                foreach (var cell in row)
                {
                    if (cell == "Floor" || cell == "Door" || cell == "PLAYER" || cell == "START")
                        return true;
                }
            }

            return false; // alle andere tiles (Wall, Water) blokkeren
        }
    }
}
