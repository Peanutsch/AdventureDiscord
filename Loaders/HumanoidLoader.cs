using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adventure.Models.NPC;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class HumanoidLoader
    {
        public static List<NpcModel>? Load()
        {
            try
            {
                var humanoids = JsonDataManager.LoadObjectFromJson<HumanoidModel>("Data/NPC/humanoids.json");

                if (humanoids == null)
                {
                    LogService.Error("[HumanoidLoader] > Failed to load humanoids.json.\n");
                    return null;
                }

                // Combine all categories into a single list
                var allNpcs = new List<NpcModel>();
                if (humanoids.Humanoids != null)
                {
                    LogService.Info($"Adding catagory Humanoids to allNpcs: {humanoids.Humanoids.Count} Humanoids");
                    allNpcs.AddRange(humanoids.Humanoids);
                }

                if (humanoids.Undead!= null)
                {
                    LogService.Info($"Adding catagory Undead to allNpcs: {humanoids.Undead.Count} Undead");
                    allNpcs.AddRange(humanoids.Undead);
                }

                if (humanoids.Human!= null)
                {
                    LogService.Info($"Adding catagory Humanoids to allNpcs: {humanoids.Human.Count} Humans");
                    allNpcs.AddRange(humanoids.Human);
                }

                if (humanoids.Elf!= null)
                {
                    LogService.Info($"Adding catagory Elf to allNpcs: {humanoids.Elf.Count} Elfs");
                    allNpcs.AddRange(humanoids.Elf);
                }

                if (humanoids.Dwarf!= null)
                {
                    LogService.Info($"Adding catagory Dwarf to allNpcs: {humanoids.Dwarf.Count} Dwarfs");
                    allNpcs.AddRange(humanoids.Dwarf);
                }

                LogService.Info($"[HumanoidLoader] > Loaded total of {allNpcs.Count} NPCs from humanoids.json\n");

                return allNpcs;
            }
            catch (System.Exception ex)
            {
                LogService.Error($"[HumanoidLoader] > Error loading humanoids: {ex.Message}\n");
                return null;
            }
        }            
    }
}
