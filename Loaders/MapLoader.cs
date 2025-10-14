using Adventure.Models.Map;
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
            var listMap = JsonDataManager.LoadListFromJson<MapModel>("Data/Map/maps.json");

            if (listMap == null || listMap.Count == 0)
            {
                LogService.Error($"[MapLoader.Load()] No Data found in maps.json...");
            }
            else
            {
                LogService.Info($"[MapLoader.Load()] Loading {listMap.Count} map's");

                foreach (var tile in listMap!)
                {
                    LogService.Info($"[MapLoader.Load()] Map Id <{tile.Id}> loaded (Name: {tile.MapName})");
                }
            }
           
            return listMap;
        }
            
    }
}
