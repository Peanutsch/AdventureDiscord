using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adventure.Models.Creatures;

namespace Adventure.Loaders
{
    public static class HumanoidLoader
    {
        public static List<CreaturesModel>? Load() =>
            JsonDataLoader.LoadListFromJson<CreaturesModel>("Data/Creatures/humanoids.json");
    }
}
