using Adventure.Models.Items;
using Adventure.Models.Player;
using Adventure.Services;
using Discord;
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
        #region === Load List from Json ===
        /// <summary>
        /// Loads a list of objects from a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of objects in the list.</typeparam>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>List of deserialized objects or null if deserialization fails.</returns>
        public static List<T>? LoadListFromJson<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);

            LogService.Info($"[JsonDataManager.LoadListFromJson] Method LoadListFromJson is called...\n" +
                            $"> Returning [LIST] of filepath: {filePath}...");

            return JsonSerializer.Deserialize<List<T>>(json);
        }
        #endregion

        #region === Load Object from Json ===
        /// <summary>
        /// Loads a single object from a JSON file.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>Deserialized object or null if deserialization fails.</returns>
        public static T? LoadObjectFromJson<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);

            LogService.Info($"[JsonDataManager.LoadObjectFromJson] Method LoadObjectFromJson is called...\n" +
                            $"> Returning [OBJECT] of filepath: {filePath}...");

            return JsonSerializer.Deserialize<T>(json);
        }
        #endregion

        #region === Load Player from Json ===
        /// <summary>
        /// Loads a PlayerModel from Data/Player/{userId}.json. Returns null if file doesn't exist.
        /// </summary>
        public static PlayerModel? LoadPlayerFromJson(ulong userId)
        {
            try
            {
                //string filePath = Path.Combine(PlayerDataFolder, $"{userId}.json");
                string filePath = Path.Combine("Data", "Player", $"{userId}.json");

                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                var player = JsonSerializer.Deserialize<PlayerModel>(json);
                return player;
            }
            catch (Exception ex)
            {
                LogService.Error($"[PlayerJsonManager] Error loading player {userId}: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region === Save New Player to Json ===
        public static readonly string PlayerDataFolder = Path.Combine(GetProjectRoot(), "Data", "Player");

        /// <summary>
        /// Returns the project root folder based on AppContext.BaseDirectory.
        /// Works for Debug/Release builds.
        /// </summary>
        private static string GetProjectRoot()
        {
            string? baseDir = AppContext.BaseDirectory;
            string projectRoot = baseDir;

            // Go up 3 levels (bin/Debug/net9.0 => project root)
            for (int i = 0; i < 4; i++)
                projectRoot = Path.GetDirectoryName(projectRoot) ?? projectRoot;

            return projectRoot;
        }

        /// <summary>
        /// Base folder for player profiles relative to project root.
        /// </summary>
        /// <param name="userId">Discord user ID.</param>
        /// <param name="player">The player model to save.</param>
        public static void SaveNewPlayerToJson(ulong userId, PlayerModel player)
        {
            //string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Player", $"{userId}.json");
            //string? directory = Path.GetDirectoryName(filePath);

            try
            {
                if (!Directory.Exists(PlayerDataFolder))
                    Directory.CreateDirectory(PlayerDataFolder);

                //string filePath = Path.Combine(PlayerDataFolder, $"{userId}.json");
                string filePath = Path.Combine("Data", "Player", $"{userId}.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(player, options);
                File.WriteAllText(filePath, json);

                LogService.Info($"[PlayerJsonManager] Saved player {userId} to {filePath}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[SaveToJson] Error saving player data for {userId}:\n{ex.Message}");
            }
        }
        #endregion

        #region === Update Player Data ===
        /// <summary>
        /// Updates the hitpoints value in the player's JSON file.
        /// </summary>
        /// <param name="userId">Discord user ID.</param>
        /// <param name="newHitpoints">New hitpoints value to set.</param>
        public static void UpdatePlayerHitpoints(ulong userId, string playerName, int newHitpoints)
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
        public static void UpdatePlayerItems(ulong userId, string playerName, string listType, string itemId)
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

        /// <summary>
        /// Updates the XP value in the player's JSON file.
        /// </summary>
        /// <param name="userId">Discord user ID.</param>
        /// <param name="newXP">New hitpoints value to set.</param>
        public static void UpdatePlayerXP(ulong userId, string playerName, int newXP)
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
                    LogService.Error("[UpdatePlayerXPInJson] Failed to deserialize PlayerModel.");
                    return;
                }

                player.XP = newXP;

                string updatedJson = JsonSerializer.Serialize(player, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(path, updatedJson);

                LogService.Info($"[UpdatePlayerXPInJson] Player XP updated to {newXP} for userId {userId} ({playerName})");
            }
            catch (Exception ex)
            {
                LogService.Error($"[UpdatePlayerHitpointsInJson] Exception: {ex.Message}");
            }
        }

        public static void UpdatePlayerLevel(ulong userId, string playerName, int newLevel)
        {
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[UpdatePlayerLevelInJson] File not found: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var player = JsonSerializer.Deserialize<PlayerModel>(json);

                if (player == null)
                {
                    LogService.Error("[UpdatePlayerLevelInJson] Failed to deserialize PlayerModel.");
                    return;
                }

                player.Level = newLevel;

                string updatedJson = JsonSerializer.Serialize(player, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(path, updatedJson);

                LogService.Info($"[UpdatePlayerLevelInJson] Player level updated to {newLevel} for userId {userId} ({playerName})");
            }
            catch (Exception ex)
            {
                LogService.Error($"[UpdatePlayerLevelInJson] Exception: {ex.Message}");
            }
        }

        public static void UpdatePlayerSavepoint(ulong userId, string savepoint)
        {
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[UpdatePlayerLevelInJson] File not found: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var player = JsonSerializer.Deserialize<PlayerModel>(json);

                if (player == null)
                {
                    LogService.Error("[UpdatePlayerLevelInJson] Failed to deserialize PlayerModel.");
                    return;
                }

                player.Savepoint = savepoint;

                string updatedJson = JsonSerializer.Serialize(player, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(path, updatedJson);

                LogService.Info($"[UpdatePlayerLevelInJson] Savepoint updated to {savepoint} for userId {userId} ({player.Name})");
            }
            catch (Exception ex)
            {
                LogService.Error($"[UpdatePlayerLevelInJson] Exception: {ex.Message}");
            }
        }
        #endregion
    }
}
