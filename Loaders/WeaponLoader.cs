using Adventure.Models.Items;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class WeaponLoader
    {
        public static List<WeaponModel>? Load()
        {
            try
            {
                var combinedWeapons = new List<WeaponModel>();

                var arrows = JsonDataManager.LoadListFromJson<WeaponModel>("Data/Items/Weapons/arrows.json");
                if (arrows != null)
                {
                    LogService.Info($"Adding catagory Arrows to combinedWeapons: {arrows.Count} of arrows");
                    combinedWeapons.AddRange(arrows);
                }

                var melee = JsonDataManager.LoadListFromJson<WeaponModel>("Data/Items/Weapons/melee.json");
                if (melee != null)
                {
                    LogService.Info($"Adding catagory Melee to combinedWeapons: {melee.Count} of melee weapons");
                    combinedWeapons.AddRange(melee);
                }


                var range = JsonDataManager.LoadListFromJson<WeaponModel>("Data/Items/Weapons/range.json");
                if (range != null)
                {
                    LogService.Info($"Adding catagory Range to combinedWeapons: {range.Count} of ranged weapons");
                    combinedWeapons.AddRange(range);
                }

                /*
                var throwWeapons = JsonDataLoader.LoadListFromJson<WeaponModel>("Data/Items/Weapons/throw.json");
                if (throwWeapons != null)
                {
                    LogService.Info($"Adding catagory Trow Weapons to combinedWeapons: {trowWeapons.Count} of trown weapons");
                    combinedWeapons.AddRange(throwWeapons);
                }
                */

                LogService.Info($"Loaded total of {combinedWeapons.Count} weapons\n");

                return combinedWeapons;
            }
            catch (System.Exception ex)
            {
                LogService.Error($"[WeaponLoader] > Error loading weapons: {ex.Message}\n");
                return null;
            }
        }

    }
}
