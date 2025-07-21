using Adventure.Models.Items;
using Adventure.Models.Player;
using Adventure.Services;
using System;
using System.Linq;
using System.Text.Json;

namespace Adventure.Loaders
{
    /// <summary>
    /// Handles loading and saving JSON data for player models and item lists.
    /// </summary>
    public static class JsonDataManager
    {
        /// <summary>
        /// Loads a list of objects from a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of objects in the list.</typeparam>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>List of deserialized objects or null if deserialization fails.</returns>
        public static List<T>? LoadListFromJson<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json);
        }

        /// <summary>
        /// Loads a single object from a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>Deserialized object or null if deserialization fails.</returns>
        public static T? LoadObjectFromJson<T>(string filePath)
        {
            LogService.Info($"[JsonDataManager.LoadObjectFromJson] Method LoadObjectFromJson is called.\nparam filepath: {filePath}");

            var json = File.ReadAllText(filePath);

            LogService.Info($"[JsonDataManager.LoadObjectFromJson] Returning object\n{json}");

            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Saves the full PlayerModel to a JSON file in the Data/Player directory.
        /// </summary>
        /// <param name="userId">Discord user ID.</param>
        /// <param name="player">The player model to save.</param>
        public static void SaveToJson(ulong userId, PlayerModel player)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Player", $"{userId}.json");
            string? directory = Path.GetDirectoryName(filePath);

            LogService.Info($"[JsonDataManager.SaveToJson] Directory: {directory}");
            try
            {
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(player, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                LogService.Error($"[SaveToJson] Error saving player data for {userId}:\n{ex.Message}");
            }
        }

        /// <summary>
        /// Updates the hitpoints value in the player's JSON file.
        /// </summary>
        /// <param name="userId">Discord user ID.</param>
        /// <param name="newHitpoints">New hitpoints value to set.</param>
        public static void UpdatePlayerHitpointsInJson(ulong userId, string playerName, int newHitpoints)
        {
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[UpdatePlayerHitpointsInJson] File not found: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var player = JsonSerializer.Deserialize<PlayerModel>(json);

                if (player == null)
                {
                    LogService.Error("[UpdatePlayerHitpointsInJson] Failed to deserialize PlayerModel.");
                    return;
                }

                player.Hitpoints = newHitpoints;

                string updatedJson = JsonSerializer.Serialize(player, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(path, updatedJson);

                LogService.Info($"[UpdatePlayerHitpointsInJson] Hitpoints updated to {newHitpoints} for userId {userId} ({playerName})");
            }
            catch (Exception ex)
            {
                LogService.Error($"[UpdatePlayerHitpointsInJson] Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new inventory entry (item, weapon, or loot) with a default value to the specified list in the player's JSON file.
        /// </summary>
        /// <param name="userId">Discord user ID.</param>
        /// <param name="listType">The type of list to update: "items", "weapons", or "loot".</param>
        /// <param name="itemId">The item identifier (ID) to add to the list.</param>
        public static void UpdatePlayerItemsInJson(ulong userId, string playerName, string listType, string itemId)
        {
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[UpdatePlayerItemsInJson] File not found: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var player = JsonSerializer.Deserialize<PlayerModel>(json);

                if (player == null)
                {
                    LogService.Error("[UpdatePlayerItemsInJson] Failed to deserialize PlayerModel.");
                    return;
                }

                switch (listType.ToLower())
                {
                    case "items":
                        if (!player.Items.Any(i => i.Id == itemId))
                        {
                            player.Items.Add(new PlayerInventoryItemModel { Id = itemId, Value = 1 });
                        }
                        break;

                    case "weapons":
                        if (!player.Weapons.Any(w => w.Id == itemId))
                        {
                            player.Weapons.Add(new PlayerInventoryWeaponsModel { Id = itemId, Value = 1 });
                        }
                        break;

                    case "loot":
                        if (!player.Loot.Any(l => l.Id == itemId))
                        {
                            player.Loot.Add(new PlayerInventoryItemModel { Id = itemId, Value = 1 });
                        }
                        break;

                    default:
                        LogService.Error($"[UpdatePlayerItemsInJson] Unknown listType '{listType}'");
                        return;
                }

                string updatedJson = JsonSerializer.Serialize(player, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(path, updatedJson);
                LogService.Info($"[UpdatePlayerItemsInJson] Item '{itemId}' added to '{listType}' for userId {userId} ({playerName})");
            }
            catch (Exception ex)
            {
                LogService.Error($"[UpdatePlayerItemsInJson] Exception: {ex.Message}");
            }
        }
    }
}
