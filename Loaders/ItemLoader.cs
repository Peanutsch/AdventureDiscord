using Adventure.Models.Items;

namespace Adventure.Loaders
{
    public static class ItemLoader
    {
        public static List<ItemModel>? Load()
        {
            var combinedItems = new List<ItemModel>();

            var healingPotions = JsonDataManager.LoadListFromJson<ItemModel>("Data/Items/Potions/healing.json");
            if (healingPotions != null)
                combinedItems.AddRange(healingPotions);

            var poisonPotions = JsonDataManager.LoadListFromJson<ItemModel>("Data/Items/Potions/poisening.json");
            if (poisonPotions != null)
                combinedItems.AddRange(poisonPotions);

            var valuables = JsonDataManager.LoadListFromJson<ItemModel>("Data/Items/Valuables/valuables.json");
            if (valuables != null)
                combinedItems.AddRange(valuables);

            return combinedItems;
        }
    }
}
