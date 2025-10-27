using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Loaders
{
    public class TestHouseLoader
    {
        // Flattened list of all tiles
        public static List<TileModel> AllTiles { get; set; } = new();

        // Quick lookup by "AreaId:TileId"
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // Tiles grouped by area
        public static Dictionary<string, List<TileModel>> AreaTiles { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // Area metadata
        public static Dictionary<string, TestHouseAreaModel> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads the house layout and merges tile details.
        /// </summary>
        public static List<TileModel>? Load()
        {
            try
            {
                var houseLayout = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");
                var tileDetails = JsonDataManager.LoadObjectFromJson<TestHouseTilesModel>("Data/Map/TestHouse/testhousetiles.json");

                if (houseLayout?.Areas == null || houseLayout.Areas.Count == 0)
                {
                    LogService.Error("[TestHouseLoader] > Failed to load testhouse.json or no areas found.");
                    return null;
                }

                if (tileDetails?.Areas == null || tileDetails.Areas.Count == 0)
                {
                    LogService.Error("[TestHouseLoader] > Failed to load testhousetiles.json or no areas found.");
                }

                // Clear previous data
                AllTiles.Clear();
                TileLookup.Clear();
                AreaTiles.Clear();
                AreaLookup.Clear();

                foreach (var areaKvp in houseLayout.Areas)
                    ProcessArea(areaKvp.Key, areaKvp.Value, tileDetails!);

                LogService.Info($"[TestHouseLoader] > Loaded {AreaLookup.Count} areas");
                LogService.Info($"[TestHouseLoader] > Loaded {AllTiles.Count} tiles\n");

                return AllTiles;
            }
            catch (Exception ex)
            {
                LogService.Error($"[TestHouseLoader] > Error loading JSON files:\n{ex}");
                return null;
            }
        }

        /// <summary>
        /// Process an area: generate TileModels from layout strings and merge tile details.
        /// </summary>
        private static void ProcessArea(string areaName, TestHouseAreaModel area, TestHouseTilesModel tileDetails)
        {
            if (area == null)
            {
                LogService.Error($"[TestHouseLoader.ProcessArea] Area {areaName} is null!");
                return;
            }

            LogService.Info($"[TestHouseLoader.ProcessArea] Processing areaName: {areaName}");

            AreaLookup[area.Id] = area;

            var areaTileList = new List<TileModel>();
            int tileCounter = 0;

            // Get details for this area
            var areaTileDetails = tileDetails?.Areas.ContainsKey(area.Id) == true
                ? tileDetails.Areas[area.Id]
                : new List<TestHouseTileDetailModel>();

            if (area.Layout != null && area.Layout.Count > 0)
            {
                for (int row = 0; row < area.Layout.Count; row++)
                {
                    for (int col = 0; col < area.Layout[row].Count; col++)
                    {
                        string tileType = area.Layout[row][col];

                        var tile = new TileModel
                        {
                            TileId = $"{row}_{col}",
                            TileName = tileType,
                            TilePosition = $"{row},{col}",
                            AreaId = area.Id
                        };

                        // Merge details from testhousetiles.json
                        var detail = areaTileDetails.Find(t => t.Id.Equals(tileType, StringComparison.OrdinalIgnoreCase));
                        if (detail != null)
                        {
                            tile.TileText = detail.Text;
                            tile.Overlays = detail.Overlays ?? new List<string>();
                            tile.TilePOI = detail.Pois ?? new List<string>();
                            tile.TileItems = detail.Items ?? new List<string>();
                            tile.Connections = detail.Connections ?? new List<string>();
                        }
                        else
                        {
                            tile.TileText = tileType; // fallback if no detail found
                            tile.Overlays = new List<string>();
                            tile.TilePOI = new List<string>();
                            tile.TileItems = new List<string>();
                            tile.Connections = new List<string>();
                        }

                        areaTileList.Add(tile);
                        AllTiles.Add(tile);

                        string key = $"{area.Id}:{tile.TileId}";
                        TileLookup[key] = tile;

                        tileCounter++;
                    }
                }
            }
            else
            {
                LogService.Error($"[TestHouseLoader.ProcessArea] Area {areaName} has no layout, skipping tiles.");
            }

            AreaTiles[areaName] = areaTileList;
            LogService.Info($"[TestHouseLoader.ProcessArea] Loaded {tileCounter} tiles for area {areaName}");
        }
    }
}
