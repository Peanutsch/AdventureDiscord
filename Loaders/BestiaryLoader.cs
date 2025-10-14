using Adventure.Models.NPC;
using Adventure.Services;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Loaders {
    public static class BestiaryLoader {
        /// <summary>
        /// Loads the full bestiary JSON, including mammals, birds, reptiles, and magical beasts,
        /// and combines all NPCs into a single list.
        /// </summary>
        /// <returns>A list of all NpcModel instances, or null if loading fails.</returns>
        public static List<NpcModel>? Load() 
        {
            try 
            {
                var beasts = JsonDataManager.LoadObjectFromJson<BestiaryModel>("Data/NPC/bestiary.json");

                if (beasts == null) {
                    LogService.Error("[BestiaryLoader] > Failed to load BestiaryContainer.");
                    return null;
                }

                // Combine all categories into a single list
                var allNpcs = new List<NpcModel>();
                if (beasts.Mammals != null)
                {
                    LogService.Info($"Adding catagory Mammals: {beasts.Mammals} Mammals");
                    allNpcs.AddRange(beasts.Mammals);
                }
                    
                if (beasts.Birds != null)
                {
                    LogService.Info($"Adding catagory Birds: {beasts.Birds} Birds");
                    allNpcs.AddRange(beasts.Birds);
                }
                    
                if (beasts.Reptiles != null)
                {
                    LogService.Info($"Adding catagory Reptiles: {beasts.Reptiles} Reptiles");
                    allNpcs.AddRange(beasts.Reptiles);
                }
                    
                if (beasts.MagicalBeasts != null)
                {
                    LogService.Info($"Adding catagory MagicalBeasts: {beasts.MagicalBeasts} Magical Beasts");
                    allNpcs.AddRange(beasts.MagicalBeasts);
                }
                    
                LogService.Info($"[BestiaryLoader] > Loaded total of {allNpcs.Count} NPCs from bestiary.json");

                return allNpcs;
            }
            catch (System.Exception ex) {
                LogService.Error($"[BestiaryLoader] > Error loading bestiary: {ex.Message}");
                return null;
            }
        }
    }
}
