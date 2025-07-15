using Adventure.Models.Items;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class WeaponLoader
    {
        public static List<WeaponModel> Load()
        {
            var combinedWeapons = new List<WeaponModel>();

            var arrows = JsonDataManager.LoadListFromJson<WeaponModel>("Data/Items/Weapons/arrows.json");
            if (arrows != null)
            {
                LogService.Info($"[WeaponLoader] > Adding [arrows]: {arrows.Count} to GameData.Weapons");
                combinedWeapons.AddRange(arrows);
            }

            var melee = JsonDataManager.LoadListFromJson<WeaponModel>("Data/Items/Weapons/melee.json");
            if (melee != null)
            {
                LogService.Info($"[WeaponLoader] > Adding [melee]: {melee.Count} to GameData.Weapons");
                combinedWeapons.AddRange(melee);
            }
                

            var range = JsonDataManager.LoadListFromJson<WeaponModel>("Data/Items/Weapons/range.json");
            if (range != null)
            {
                LogService.Info($"[WeaponLoader] > Adding [range]: {range.Count} to GameData.Weapons");
                combinedWeapons.AddRange(range);
            }
                
            /*
            var throwWeapons = JsonDataLoader.LoadListFromJson<WeaponModel>("Data/Items/Weapons/throw.json");
            if (throwWeapons != null)
            {
                LogService.Info($"[WeaponLoader] > Adding [throwWeapons]: {throwWeapons.Count} to GameData.Weapons");
                combinedWeapons.AddRange(throwWeapons);
            }
            */

            return combinedWeapons;
        }

    }
}
