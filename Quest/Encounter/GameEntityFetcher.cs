using Adventure.Data;
using Adventure.Models.Items;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Encounter
{
    /// <summary>
    /// Provides helper methods to resolve game entity IDs into their full attribute models,
    /// such as armor, weapons, and items.
    /// </summary>
    public static class GameEntityFetcher
    {
        #region === Retrieve Armor Attributes ===
        /// <summary>
        /// Resolves a list of armor IDs into their corresponding ArmorModel objects.
        /// </summary>
        /// <param name="armorIds">List of armor IDs to resolve.</param>
        /// <returns>List of ArmorModel objects corresponding to the provided IDs.</returns>
        public static List<ArmorModel> RetrieveArmorAttributes(List<string> armorIds)
        {
            LogService.Info($"[GameEntityFetcher.RetrieveArmorAttributes] > Resolving armor names for IDs: {string.Join(", ", armorIds)}");

            // Return empty list if there are no IDs provided
            if (armorIds.Count >= 1)
            {
                if (GameData.Armor == null)
                {
                    LogService.Error("[GameEntityFetcher.RetrieveArmorAttributes] > GameData.Armor is null. Armor data was not loaded.");
                    return new List<ArmorModel>();
                }

                // Match each ID to an armor model in GameData.Armor
                var armors = armorIds
                    .Select(id =>
                    {
                        var armor = GameData.Armor.FirstOrDefault(a => a.Id == id);
                        if (armor == null)
                            LogService.Error($"[GameEntityFetcher.RetrieveArmorAttributes] > Armor with ID '{id}' not found.");
                        return armor;
                    })
                    .Where(a => a != null) // Filter out nulls
                    .ToList();

                // Log each resolved armor for debugging
                int counter = 1;
                foreach (var item in armors)
                {
                    LogService.Info($"Armor Item #{counter}: {item}");
                    counter++;
                }

                return armors!;
            }

            LogService.Error("[GameEntityFetcher.RetrieveArmorAttributes] > armorIds.Count is 0...");
            return new List<ArmorModel>();
        }
        #endregion

        #region === Retrieve Weapon Attributes ===
        /// <summary>
        /// Resolves a list of weapon IDs into their corresponding WeaponModel objects.
        /// </summary>
        /// <param name="weaponIds">List of weapon IDs to resolve.</param>
        /// <returns>List of WeaponModel objects corresponding to the provided IDs.</returns>
        public static List<WeaponModel> RetrieveWeaponAttributes(List<string> weaponIds)
        {
            // Return empty list if null or empty
            if (weaponIds == null || weaponIds.Count == 0)
                return new List<WeaponModel>();

            LogService.Info($"[GameEntityFetcher.RetrieveWeaponAttributes] > Resolving weapon data for IDs: {string.Join(", ", weaponIds)}");

            if (GameData.Weapons == null)
            {
                LogService.Error("[GameEntityFetcher.RetrieveWeaponAttributes] > GameData.Weapons is null. Weapon data was not loaded.");
                return new List<WeaponModel>();
            }

            // Match each ID to a weapon model in GameData.Weapons
            var weapons = weaponIds
                .Select(id =>
                {
                    var weapon = GameData.Weapons!.FirstOrDefault(w => w.Id == id);
                    if (weapon == null)
                        LogService.Error($"[GameEntityFetcher.RetrieveWeaponAttributes] > Weapon with ID '{id}' not found.");
                    return weapon;
                })
                .Where(w => w != null) // Filter out nulls
                .ToList()!;

            // Log each resolved weapon for debugging
            int counter = 1;
            foreach (var item in weapons)
            {
                LogService.Info($"Weapon Item #{counter}: {item!.Name}");
                counter++;
            }

            return weapons!;
        }
        #endregion

        #region === Retrieve Item Attributes ===
        /// <summary>
        /// Resolves a list of item IDs into their corresponding ItemModel objects.
        /// </summary>
        /// <param name="itemIds">List of item IDs to resolve.</param>
        /// <returns>List of ItemModel objects corresponding to the provided IDs.</returns>
        public static List<ItemModel> RetrieveItemAttributes(List<string> itemIds)
        {
            // Return empty list if null or empty
            if (itemIds == null || itemIds.Count == 0)
                return new List<ItemModel>();

            LogService.Info($"[GameEntityFetcher.RetrieveItemAttributes] > Resolving Item data for IDs: {string.Join(", ", itemIds)}");

            if (GameData.Items == null)
            {
                LogService.Error("[GameEntityFetcher.RetrieveItemAttributes] > GameData.Items is null. Item data was not loaded.");
                return new List<ItemModel>();
            }

            // Match each ID to an item model in GameData.Items
            var items = itemIds
                .Select(id =>
                {
                    var item = GameData.Items!.FirstOrDefault(i => i.Id == id);
                    if (item == null)
                        LogService.Error($"[GameEntityFetcher.RetrieveItemAttributes] > Item with ID '{id}' not found.");
                    return item;
                })
                .Where(i => i != null) // Filter out nulls
                .ToList()!;

            // Log each resolved item for debugging
            int counter = 1;
            foreach (var item in items)
            {
                LogService.Info($"Item #{counter}: {item!.Name}");
                counter++;
            }

            return items!;
        }
        #endregion
    }
}
