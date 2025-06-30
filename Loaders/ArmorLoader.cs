using Adventure.Models.Items;

namespace Adventure.Loaders
{
    public static class ArmorLoader
    {
        public static List<ArmorModel> Load() =>
            JsonDataLoader.LoadListFromJson<ArmorModel>("Data/Items/armor.json");
    }
}
