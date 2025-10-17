using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Loaders
{
    public class TestRoomTilesLoader
    {
        public static TestRoomTilesModel? Load()
        {
            try
            {
                var tiles = JsonDataManager.LoadObjectFromJson<TestRoomTilesModel>("Data/Map/testroomtiles.json");

                if (tiles == null)
                {
                    LogService.Error("[TestRoomTilesLoader] > Failed to load testroomtiles.json");
                    return null;
                }

                LogService.Info($"[TestRoomTilesLoader] > Loaded {tiles.Room1.Count} tiles.\n");
                return tiles;
            }
            catch (Exception ex)
            {
                LogService.Error($"[TestRoomTilesLoader] > Error loading tiles:\n{ex}\n");
                return null;
            }
        }
    }
}
