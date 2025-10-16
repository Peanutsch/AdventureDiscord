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
                var tiles = JsonDataManager.LoadObjectFromJson<TestRoomTilesModel>("Data/Map/testhousetiles.json");

                if (tiles == null)
                {
                    LogService.Error("[TestRoomTilesLoader] > Failed to load testhousetiles.json");
                    return null;
                }

                LogService.Info($"[TestRoomTilesLoader] > Loaded {tiles.TilesRoom1.Count} tiles.\n");
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
