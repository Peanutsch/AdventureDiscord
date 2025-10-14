using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Walk
{
    public static class MapService
    {
        /// <summary>
        /// Retrieves a tile based on its ID from the loaded maps.
        /// </summary>
        public static TileModel? GetTileById(string tileId)
        {
            var tile = GameData.Maps?.FirstOrDefault(t => t.TileId == tileId);
            if (tile == null)
                LogService.Error($"[MapService.GetTileById] Tile '{tileId}' not found.");
            return tile;
        }

        /// <summary>
        /// Returns all possible exits from a given tile in a Dictionary<direction, connection>
        /// e.g. <North, tile D>
        /// </summary>
        public static Dictionary<string, string> GetExits(TileModel map)
        {
            if (map.TileExits == null)
                return new Dictionary<string, string>();

            return map.TileExits
                .GetType()
                .GetProperties()
                .Select(p => new { Direction = p.Name, Target = p.GetValue(map.TileExits) as string })
                .Where(x => !string.IsNullOrEmpty(x.Target))
                .ToDictionary(x => x.Direction, x => x.Target!);
        }

        /// <summary>
        /// Logs all possible connections of a tile.
        /// </summary>
        public static void LogConnections(TileModel map)
        {
            var connections = GetExits(map);
            if (!connections.Any())
            {
                LogService.Info($"[MapService.LogConnections] {map.TileName} has no available connections.");
                return;
            }

            foreach (var connection in connections)
            {
                LogService.Info($"[MapService.LogConnections] Direction {connection.Key}: {connection.Value}");
            }
        }
    }
}
