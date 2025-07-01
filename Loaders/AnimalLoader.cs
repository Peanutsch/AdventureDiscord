using Adventure.Models.Creatures;

namespace Adventure.Loaders
{
    public static class AnimalLoader
    {
        public static List<CreaturesModel>? Load() =>
            JsonDataLoader.LoadListFromJson<CreaturesModel>("Data/Creatures/animals.json");
    }
}
