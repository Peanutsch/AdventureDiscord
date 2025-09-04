using Adventure.Models.Creatures;

namespace Adventure.Loaders
{
    public static class AnimalLoader
    {
        public static List<NpcModel>? Load() =>
            JsonDataManager.LoadListFromJson<NpcModel>("Data/Creatures/animals.json");
    }
}
