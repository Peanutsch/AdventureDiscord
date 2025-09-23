using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adventure.Models.NPC;

namespace Adventure.Loaders
{
    public static class HumanoidLoader
    {
        public static List<NpcModel>? Load() =>
            JsonDataManager.LoadListFromJson<NpcModel>("Data/NPC/humanoids.json");
    }
}
