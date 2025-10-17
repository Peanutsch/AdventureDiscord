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
    public static class MapLoader
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
                var maps = JsonDataManager.LoadObjectFromJson<MapModel>("Data/Map/maps.json");

                if (maps == null)
                {
                    LogService.Error("[MapLoader] > Failed to load maps.json");
                    return null;
                }

                var allMaps = new List<TileModel>();

                if (maps.TestMap1 != null)
                {
                    LogService.Info($"[MapLoader] > Adding category TestMap1: {maps.TestMap1.Count} tiles");
                    allMaps.AddRange(maps.TestMap1);
                }

                /*
                if (maps.TestMap2 != null)
                {
                    LogService.Info($"[MapLoader] > Adding category TestMap2: {maps.TestMap2.Count} tiles");
                    GetMapDimensions.AssignMapDimensions(maps.TestMap2, "TestMap2");
                    allMaps.AddRange(maps.TestMap2);
                }
                */

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
