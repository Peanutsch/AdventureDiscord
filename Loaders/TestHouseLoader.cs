using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Loaders
{
    public class TestHouseLoader
    {
        public static List<TileModel> TestAllTiles { get; set; } = new();
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, List<TileModel>> AreaTiles { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, TestHouseAreaModel> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public static List<TileModel>? Load()
        {
            try
            {
                var testHouse = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");

                if (testHouse?.Areas == null || testHouse.Areas.Count == 0)
                {
                    LogService.Error("[TestHouseLoader] > Failed to load testhouse.json or no areas found.");
                    return null;
                }

                // Clear previous data
                TestAllTiles.Clear();
                TileLookup.Clear();
                AreaTiles.Clear();
                AreaLookup.Clear();

                // Process each area
                foreach (var area in testHouse.Areas)
                    ProcessArea(area.Key, area.Value);

                LogService.Info($"[TestHouseLoader] > Loaded {AreaLookup.Count} areas");
                LogService.Info($"[TestHouseLoader] > Loaded {TestAllTiles.Count} tiles\n");

                return TestAllTiles;
            }
            catch (Exception ex)
            {
                LogService.Error($"[TestHouseLoader] > Error loading testhouse.json:\n{ex}");
                return null;
            }

            static void ProcessArea(string areaName, TestHouseAreaModel area)
            {
                if (area == null)
                {
                    LogService.Error($"[TestHouseLoader.ProcessArea] Area {areaName} is null!");
                    return;
                }

                LogService.Info($"[TestHouseLoader.ProcessArea] Processing area: {areaName}");

                AreaLookup[area.Id] = area;
                var areaTileList = new List<TileModel>();
                int tileCounter = 0;

                if (area.Layout != null)
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
                                TileText = tileType,
                                AreaId = area.Id
                            };

                            areaTileList.Add(tile);
                            TestAllTiles.Add(tile);

                            string key = $"{area.Id}:{tile.TileId}";
                            TileLookup[key] = tile;

                            tileCounter++;
                        }
                    }
                }

                AreaTiles[areaName] = areaTileList;
                LogService.Info($"[TestHouseLoader.ProcessArea] Loaded {tileCounter} tiles for area {areaName}");
            }
        }
    }
}
