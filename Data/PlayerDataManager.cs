using Adventure.Loaders;
using Adventure.Models.Attributes;
using Adventure.Models.Player;
using Adventure.Services;
using Discord;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Data {
    /// <summary>
    /// Handles loading, saving, and managing player data in the system.
    /// </summary>
    public static class PlayerDataManager 
    {
        /// <summary>
        /// Loads a player's data by their Discord user ID.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        /// <returns>The loaded PlayerModel, or a new empty PlayerModel if not found.</returns>
        public static PlayerModel LoadByUserId(ulong userId) {
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            if (!File.Exists(path)) {
                LogService.Error($"[PlayerDataManager.LoadByUserId] Player file not found for userId {userId}. Returning empty PlayerModel.");
                return new PlayerModel { Id = userId };
            }

            var player = JsonDataManager.LoadObjectFromJson<PlayerModel>(path);
            return player ?? new PlayerModel { Id = userId };
        }

        /// <summary>
        /// Loads all player data files from the player data directory.
        /// </summary>
        /// <returns>A list of all PlayerModel objects found, or an empty list if none exist.</returns>
        public static List<PlayerModel>? LoadAll() {
            string directoryPath = "Data/Player";

            if (!Directory.Exists(directoryPath))
                return new List<PlayerModel>();

            var players = new List<PlayerModel>();
            var files = Directory.GetFiles(directoryPath, "*.json");

            foreach (var file in files) {
                var player = JsonDataManager.LoadObjectFromJson<PlayerModel>(file);
                if (player != null) {
                    players.Add(player);
                }
            }

            return players;
        }

        /// <summary>
        /// Generates a unique player name by appending a random number if the base name already exists.
        /// </summary>
        /// <param name="baseName">The base name to use as a starting point.</param>
        /// <returns>A unique player name not used by any other player.</returns>
        public static string GenerateUniquePlayerName(string baseName) {
            var players = LoadAll();
            var existingNames = players?.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

            string uniqueName = baseName;
            var rand = new Random();

            // Continue generating a random suffix until a unique name is found
            while (existingNames.Contains(uniqueName)) {
                LogService.Error($"[PlayerDataManager.GenerateUniquePlayerName] Player Name {uniqueName} already exists. Creating unique player name...");
                uniqueName = $"{baseName}#{rand.Next(1000, 9999)}";

                LogService.Error($"[PlayerDataManager.GenerateUniquePlayerName] Unique Player Name: {uniqueName}");
            }

            return uniqueName;
        }

        /// <summary>
        /// Creates a default player for a given user ID and player name.
        /// Loads default_template_player.json if available, otherwise uses hardcoded defaults.
        /// </summary>
        /// <remarks>
        /// Default player template reference for new players:
        /// --------------------------------------------------
        /// Attributes:
        /// | Attribute    | Default Value |
        /// |--------------|---------------|
        /// | Strength     | 10            |
        /// | Dexterity    | 14            |
        /// | Constitution | 10            |
        /// | Intelligence | 10            |
        /// | Wisdom       | 11            |
        /// | Charisma     | 10            |
        /// --------------------------------------------------
        /// Weapons:
        /// | Slot | Id  | Value |
        /// |------|-----|-------|
        /// | 1    | "weapon_short_sword"  | 1     |  
        /// --------------------------------------------------
        /// Armor:
        /// | Slot | Id  | Value |
        /// |------|-----|-------|
        /// | 1    | "armor_hide_armor"  | 1     |  
        /// --------------------------------------------------
        /// Items: empty list by default
        /// Loot: empty list by default
        /// MaxHitpoints: 1000 (temp. hp)
        /// MaxCarry: 70
        /// --------------------------------------------------
        /// Notes:
        /// - If default_template_player.json is found, these values are loaded from it.
        /// - Unique player names are generated if base name already exists.
        /// - This template is applied when a player is created for the first time.
        /// </remarks>
        public static PlayerModel CreateNewPlayer(ulong userId, string playerName) {
            LogService.Info("[CreateDefaultPlayer] Attempting to load default_template_player.json");

            var defaultTemplate = JsonDataManager.LoadObjectFromJson<PlayerModel>("Data/Player/default_template_player.json");

            if (defaultTemplate != null)
                LogService.Info("[CreateDefaultPlayer] Finished loading default template");

            var player = defaultTemplate ?? new PlayerModel {
                Id = userId,
                Name = playerName,
                Hitpoints = 1000,
                MaxCarry = 70,
                Savepoint = "START",
                Attributes = new AttributesModel {
                    Strength = 10,
                    Dexterity = 14,
                    Constitution = 10,
                    Intelligence = 10,
                    Wisdom = 11,
                    Charisma = 10
                },

                Weapons = new List<PlayerInventoryWeaponsModel>
                {
                    new PlayerInventoryWeaponsModel
                    {
                        Id = "weapon_short_sword",
                        Value = 1
                    }
                },

                Armor = new List<PlayerInventoryArmorModel>() 
                {
                    new PlayerInventoryArmorModel() 
                    {
                        Id = "armor_hide_armor",
                        Value =1
                    }
                },

                Items = new List<PlayerInventoryItemModel>(),

                Loot = new List<PlayerInventoryItemModel>()
            };

            player.Id = userId;
            player.Name = GenerateUniquePlayerName(playerName);

            JsonDataManager.SaveNewPlayerToJson(userId, player);
            return player;
        }
    }
}
