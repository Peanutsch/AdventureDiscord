using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public class GetMapDimensions
    {
        /// <summary>
        /// Berekent de hoogte en breedte van de map en kent deze toe aan alle tiles.
        /// </summary>
        public static void AssignMapDimensions(List<TileModel> tiles, string mapName)
        {
            int maxRow = 0;
            int maxCol = 0;

            foreach (var tile in tiles)
            {
                if (string.IsNullOrWhiteSpace(tile.TilePosition))
                    continue;

                var parts = tile.TilePosition.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int row) &&
                    int.TryParse(parts[1], out int col))
                {
                    if (row > maxRow) maxRow = row;
                    if (col > maxCol) maxCol = col;
                }
            }

            foreach (var tile in tiles)
            {
                tile.MapHeight = maxRow;
                tile.MapWidth = maxCol;
            }

            LogService.Info($"[MapLoader] > {mapName}: Detected map size {maxRow} rows × {maxCol} columns");
        }
    }
}
