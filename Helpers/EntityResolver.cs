using Adventure.Data;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Helpers
{
    public static class EntityResolver
    {
        public static List<string> ResolveArmorAttributes(List<string> armorIds)
        {
            if (armorIds.Count >= 1)
            {
                LogService.Info($"[EntityResolver.ResolveArmorNames] > Resolving armor names for IDs: {string.Join(", ", armorIds)}");

                if (GameData.Armor == null)
                {
                    LogService.Error("[EntityResolver.ResolveArmorNames] > GameData.Armor is null. Armor data was not loaded.");
                    return new List<string>();
                }

                var armors = armorIds
                    .Select(id =>
                    {
                        var armor = GameData.Armor.FirstOrDefault(a => a.Id == id);
                        if (armor == null)
                            LogService.Error($"[EntityResolver.ResolveArmorNames] > Armor with ID '{id}' not found.");
                        return armor;
                    })
                    .Where(a => a != null)
                    .ToList();

                var armorData = armors
                    .Select(armor => $"[{armor!.Name}]\n" +
                                     $"Type: {armor.Type} armor\n" +
                                     $"Armor Class: +{armor.AC_Bonus}\n" +
                                     $"Weight: {armor.Weight}kg\n" +
                                     $"~{armor.Description}~\n")
                    .ToList();

                return armorData;
            }

            LogService.Error("[EntityResolver.ResolveArmorNames] > armorIds.Count is 0...");
            return new List<string>();
        }

        public static List<string> ResolveWeaponAttributes(List<string?> weaponIds)
        {
            if (weaponIds == null || weaponIds.Count == 0)
            {
                LogService.Error("[EntityResolver.ResolveWeaponNames] > weaponIds is null or empty...");
                return new List<string>();
            }

            LogService.Info($"[EntityResolver.ResolveWeaponNames] > Resolving weapon names for IDs: {string.Join(", ", weaponIds.Select(id => id ?? "<null>"))}");

            if (GameData.Weapons == null || GameData.Weapons.Count == 0)
            {
                LogService.Error("[EntityResolver.ResolveWeaponNames] > GameData.Weapons is null. Weapon data was not loaded.");
                return new List<string>();
            }
            else
            {
                LogService.Info("[EntityResolver.ResolveWeaponNames] > GameData.Weapons is NOT null. Weapon data is loaded.");
                var filteredWeaponIds = weaponIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
                LogService.Info($"[EntityResolver.ResolveWeaponNames] > Filtered weapon IDs: {string.Join(", ", filteredWeaponIds)}");

                var weapons = filteredWeaponIds
                    .Select(id =>
                    {
                        LogService.Info($"[EntityResolver.ResolveWeaponNames] > Looking for weapon with ID: {id}");
                        var weapon = GameData.Weapons.FirstOrDefault(w => w.Id == id);
                        if (weapon == null)
                        {
                            LogService.Error($"[EntityResolver.ResolveWeaponNames] > Weapon with ID '{id}' not found.");
                        }
                        else
                        {
                            LogService.Info($"[EntityResolver.ResolveWeaponNames] > Found weapon: {weapon.Name}");
                        }
                        return weapon;
                    })
                    .Where(w => w != null)
                    .ToList();

                LogService.Info("[EntityResolver.ResolveWeaponNames] > Completed weapons lookup. Returning weapon data.");

                var weaponData = weapons
                    .Select(w => $"[{w!.Name}]\n" +
                                 $"Range: {w.Range}m\n" +
                                 $"Weight: {w.Weight}kg\n" +
                                 $"~{w.Description}~\n")
                    .ToList();

                return weaponData;
            }
        }
    }
}