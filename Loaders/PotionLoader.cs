using Adventure.Models.Items;

namespace Adventure.Loaders
{
    public static class PotionLoader
    {
        public static List<PotionModel>? Load()
        {
            var combinedPotions = new List<PotionModel>();

            var healingPotions = JsonDataLoader.LoadListFromJson<PotionModel>("Data/Items/Potions/healing.json");
            if (healingPotions != null)
                combinedPotions.AddRange(healingPotions);

            var poisonPotions = JsonDataLoader.LoadListFromJson<PotionModel>("Data/Items/Potions/poisening.json");
            if (poisonPotions != null)
                combinedPotions.AddRange(poisonPotions);

            return combinedPotions;
        }
    }
}
