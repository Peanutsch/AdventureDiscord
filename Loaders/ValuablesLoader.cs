using Adventure.Models.Items;

namespace Adventure.Loaders
{
    public static class ValuablesLoader
    {
        public static List<ValuablesModel>? Load() =>
            JsonDataManager.LoadListFromJson<ValuablesModel>("Data/Items/Valuables/valuables.json");
    }
}
