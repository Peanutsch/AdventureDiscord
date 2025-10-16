using Adventure.Models.Map;
using Adventure.Models.NPC;
using Adventure.Services;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Loaders
{
    public static class TileLoader
    {
        /*
        // TestMap1: 17 tiles
        // TestMap2: 4 tiles
        */

        public static List<TileModel> AllTiles { get; private set; } = new();
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new();

        public static List<TileModel>? Load()
        {
            try
            {
                var tiles = JsonDataManager.LoadObjectFromJson<MapModel>("Data/Map/maps.json");

                if (tiles == null)
                {
                    LogService.Error("[MapLoader] > Failed to load maps.json");
                    return null;
                }

                var allMaps = new List<TileModel>();

                if (tiles.TestMap1 != null)
                {
                    LogService.Info($"[MapLoader] > Adding category TestMap1: {tiles.TestMap1.Count} tiles");
                    GetMapDimensions.AssignMapDimensions(tiles.TestMap1, "TestMap1");
                    allMaps.AddRange(tiles.TestMap1);
                }

                // Tile lookup op basis van TilePosition (row,col)
                TileLookup = allMaps
                    .Where(t => !string.IsNullOrWhiteSpace(t.TilePosition))
                    .ToDictionary(t => t.TilePosition, t => t);

                AllTiles = allMaps;

                LogService.Info($"[MapLoader] > Loaded total of {allMaps.Count} maps from maps.json\n");
                return allMaps;
            }
            catch (Exception ex)
            {
                LogService.Error($"[MapLoader] > Error loading maps: {ex.Message}");
                return null;
            }
        }
    }
}
