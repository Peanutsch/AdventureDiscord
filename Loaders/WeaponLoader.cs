using Adventure.Models.Items;

namespace Adventure.Loaders
{
    public static class WeaponLoader
    {
        public static List<WeaponModel> Load()
        {
            var combinedWeapons = new List<WeaponModel>();

            combinedWeapons.AddRange(JsonDataLoader.LoadListFromJson<WeaponModel>("Data/Items/Weapons/arrows.json"));
            combinedWeapons.AddRange(JsonDataLoader.LoadListFromJson<WeaponModel>("Data/Items/Weapons/melee.json"));
            combinedWeapons.AddRange(JsonDataLoader.LoadListFromJson<WeaponModel>("Data/Items/Weapons/range.json"));
            combinedWeapons.AddRange(JsonDataLoader.LoadListFromJson<WeaponModel>("Data/Items/Weapons/throw.json"));

            return combinedWeapons;
        }
    }
}
