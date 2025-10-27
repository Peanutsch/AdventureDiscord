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
                    LogService.Error("[ArmorLoader] > Failed to load ArmorModel.\n");
                    return null;
                }

                var allArmor = new List<ArmorModel>();
                if (armor?.CraftedArmor != null)
                {
                    LogService.Info($"Adding catagory Crafted Armor to allArmor: {armor.CraftedArmor.Count} of crafted armor");
                    allArmor.AddRange(armor.CraftedArmor);
                }
                    
                if (armor?.NaturalArmor != null)
                {
                    LogService.Info($"Adding catagory Natural Armor to allArmor: {armor.NaturalArmor.Count} of natural armor");
                    allArmor.AddRange(armor.NaturalArmor);
                }
                    

                LogService.Info($"[ArmorLoader] > Loaded total of {allArmor!.Count} armor to GameData.Armor\n");

                return allArmor;
            }
            catch (System.Exception ex) 
            {
                LogService.Error($"[BestiaryLoader] > Error loading bestiary: {ex.Message}\n");
                return null;
            }
        }
    }
}
