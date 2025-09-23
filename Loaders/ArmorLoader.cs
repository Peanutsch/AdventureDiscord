using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Services;
using System.Collections.Generic;
using System.ComponentModel;

namespace Adventure.Loaders
{
    public static class ArmorLoader
    {
        public static List<ArmorModel>? Load() 
        {
            try 
            {
                var armor = JsonDataManager.LoadObjectFromJson<ArmorContainer>("Data/Items/Armor/armor.json");

                if (armor == null) {
                    LogService.Error("[ArmorLoader] > Failed to load ArmorModel.");
                    return null;
                }

                var allArmor = new List<ArmorModel>();
                if (armor?.CraftedArmor != null)
                    allArmor.AddRange(armor.CraftedArmor);
                if (armor?.NaturalArmor != null)
                    allArmor.AddRange(armor.NaturalArmor);

                LogService.Info($"[ArmorLoader] > Adding [armor]: {allArmor!.Count} to GameData.Armor");

                return allArmor;
            }
            catch (System.Exception ex) 
            {
                LogService.Error($"[BestiaryLoader] > Error loading bestiary: {ex.Message}");
                return null;
            }
        }
    }
}
