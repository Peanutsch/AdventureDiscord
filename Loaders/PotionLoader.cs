using Adventure.Models.Items;

namespace Adventure.Loaders
{
    public static class PotionLoader
    {
        public static List<PotionModel> Load()
        {
            var combinedPotions = new List<PotionModel>();

            combinedPotions.AddRange(JsonDataLoader.LoadListFromJson<PotionModel>("Data/Items/Potions/healing.json"));
            combinedPotions.AddRange(JsonDataLoader.LoadListFromJson<PotionModel>("Data/Items/Potions/poisening.json"));

            return combinedPotions;
        }
    }
}
