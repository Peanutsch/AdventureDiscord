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
        public static List<MapModel>? Load()
        {
            try
            {
                var maps = JsonDataManager.LoadObjectFromJson<TestMapModel>("Data/Map/maps.json");

                if (maps == null)
                {
                    LogService.Error("[MapLoader] > Failed to load maps.json");
                    return null;
                }

                // Combine all categories into a single list
                var allMaps = new List<MapModel>();
                if (maps.TestMap1 != null)
                {
                    LogService.Info($"Adding catagory TestMap1 to allMaps: {maps.TestMap1.Count} maps");
                    allMaps.AddRange(maps.TestMap1);
                }
                    
                if (maps.TestMap2 != null)
                {
                    LogService.Info($"Adding catagory TestMap2 to allMaps: {maps.TestMap2.Count} maps");
                    allMaps.AddRange(maps.TestMap2);
                }
                    
                LogService.Info($"[MapLoader] > Loaded total of {allMaps.Count} Maps from maps.json\n");

                return allMaps;
            }
            catch (System.Exception ex)
            {
                LogService.Error($"[MapLoader] > Error loading naps: {ex.Message}");
                return null;
            }
        }       
    }
}
