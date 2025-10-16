using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Loaders
{
    public class TestHouseLoader
    {
        public static TestHouseModel? Load()
        {
            try
            {
                var grid = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/testhouse.json");

                if (grid == null)
                {
                    LogService.Error("[TestHouseLoader] > Failed to load testhouse.json");
                    return null;
                }

                LogService.Info($"[TestHouseLoader] > Loaded {grid.Rooms.Count} rooms.\n");
                return grid;
            }
            catch (Exception ex)
            {
                LogService.Error($"[TestHouseLoader] > Error loading grid:\n{ex}\n");
                return null;
            }
        }
    }
}
