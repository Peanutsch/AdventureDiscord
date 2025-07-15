using Adventure.Data;
using Adventure.Models.Items;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Helpers
{
    public static class GameEntityFetcher
    {
        public static List<ArmorModel> RetrieveArmorAttributes(List<string> armorIds)
        {
            LogService.Info($"[GameEntityFetcher.RetrieveArmorAttributes] > Resolving armor names for IDs: {string.Join(", ", armorIds)}:");

            if (armorIds.Count >= 1)
            {
                if (GameData.Armor == null)
                {
                    LogService.Error("[GameEntityFetcher.RetrieveArmorAttributes] > GameData.Armor is null. Armor data was not loaded.");
                    return new List<ArmorModel>();
                }

                var armors = armorIds
                    .Select(id =>
                    {
                        var armor = GameData.Armor.FirstOrDefault(a => a.Id == id);
                        if (armor == null)
                            LogService.Error($"[GameEntityFetcher.RetrieveArmorAttributes] > Armor with ID '{id}' not found.");
                        return armor;
                    })
                    .Where(a => a != null)
                    .ToList();

                int counter = 1;
                foreach (var item in armors)
                {
                    LogService.Info($"Item #{counter}: {item}");
                    counter++;
                }

                return armors!;
            }

            LogService.Error("[GameEntityFetcher.RetrieveArmorAttributes] > armorIds.Count is 0...");
            return new List<ArmorModel>();
        }

        public static List<WeaponModel> RetrieveWeaponAttributes(List<string> weaponIds)
        {
            if (weaponIds == null || weaponIds.Count == 0)
                return new List<WeaponModel>();

            LogService.Info($"[GameEntityFetcher.RetrieveWeaponAttributes] > Resolving weapon data for IDs: {string.Join(", ", weaponIds)}:");

            if (GameData.Weapons == null)
            {
                LogService.Error("[GameEntityFetcher.RetrieveWeaponAttributes] > GameData.Weapons is null. Weapon data was not loaded.");
                return new List<WeaponModel>();
            }

            var weapons = weaponIds
                .Select(id =>
                {
                    var weapon = GameData.Weapons!.FirstOrDefault(w => w.Id == id);
                    if (weapon == null)
                        LogService.Error($"[EntityResolver.RetrieveWeaponAttributes] > Weapon with ID '{id}' not found.");
                    return weapon;
                })
                .Where(w => w != null)
                .ToList()!;

            int counter = 1;
            foreach (var item in weapons)
            {
                LogService.Info($"Item #{counter}: {item!.Name}");
                counter++;
            }

            return weapons!;
        }
    }
}