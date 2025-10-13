using Adventure.Models.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Loaders
{
    public static class MapLoader
    {
        public static List<MapModel>? Load() =>
            JsonDataManager.LoadListFromJson<MapModel>("Data/Map/maps.json");
    }
}
