using Adventure.Models.Items;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class ArmorLoader
    {
        public static List<ArmorModel>? Load()
        {
            var armor = JsonDataLoader.LoadListFromJson<ArmorModel>("Data/Items/Armor/armor.json");
            LogService.Info($"[ArmorLoader] > Adding [armor]: {armor!.Count} to GameData.Armor");

            return JsonDataLoader.LoadListFromJson<ArmorModel>("Data/Items/Armor/armor.json");
        }
    }
}
