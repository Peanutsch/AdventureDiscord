using Adventure.Models.Items;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class ItemLoader
    {
        public static List<ItemModel>? Load()
        {
            try
            {
                var combinedItems = new List<ItemModel>();

                var healingPotions = JsonDataManager.LoadListFromJson<ItemModel>("Data/Items/Potions/healing.json");
                if (healingPotions != null)
                {
                    LogService.Info($"Adding catagory Healing Potions: {healingPotions.Count} healing potions from healing.json");
                    combinedItems.AddRange(healingPotions);
                }

                var poisonPotions = JsonDataManager.LoadListFromJson<ItemModel>("Data/Items/Potions/poisening.json");
                if (poisonPotions != null)
                {
                    LogService.Info($"Adding catagory Poison Potions: {poisonPotions.Count} poison potions from poisening.json");
                    combinedItems.AddRange(poisonPotions);
                }


                var valuables = JsonDataManager.LoadListFromJson<ItemModel>("Data/Items/Valuables/valuables.json");
                if (valuables != null)
                {
                    LogService.Info($"Adding catagory Valuables: {valuables.Count} of valuables from valuables.json");
                    combinedItems.AddRange(valuables);
                }

                LogService.Info($"> Loaded total of {combinedItems.Count} items...\n");
                return combinedItems;
            }
            catch (System.Exception ex)
            {
                LogService.Error($"[ItemLoader] > Error loading items: {ex.Message}\n");
                return null;
            }
        }
    }
}
