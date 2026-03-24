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

namespace Adventure.Data
{
    /// <summary>
    /// Central manager for all player data operations including loading, saving, and creation.
    /// 
    /// This static class handles:
    /// - Loading individual player data by Discord user ID
    /// - Loading all registered players from disk
    /// - Creating new player accounts with default attributes and equipment
    /// - Generating unique player names to avoid duplicates
    /// - Persisting player state to JSON files
    /// 
    /// Player data is stored in JSON format in the "Data/Player" directory with filenames 
    /// following the pattern "{userId}.json" (e.g., "692450978355740796.json").
    /// 
    /// <remarks>
    /// Thread Safety: Not thread-safe. External synchronization is recommended 
    /// if accessed from multiple threads simultaneously.
    /// 
    /// Storage: All player data is persisted to disk as JSON files and loaded into memory 
    /// on demand. This allows players to resume their progress across sessions.
    /// </remarks>
    /// </summary>
    public static class PlayerDataManager
    {
        /// <summary>
        /// Loads a player's complete data from disk by their Discord user ID.
        /// 
        /// Attempts to find and deserialize a player JSON file. If the file doesn't exist
        /// or cannot be deserialized, returns a new empty PlayerModel with the given user ID.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player to load (e.g., 692450978355740796).</param>
        /// <returns>
        /// The loaded PlayerModel if found, otherwise a new empty PlayerModel with the specified ID.
        /// The returned player will always have a valid ID set.
        /// </returns>
        /// <remarks>
        /// File Path: "Data/Player/{userId}.json"
        /// 
        /// Example:
        /// var player = PlayerDataManager.LoadByUserId(692450978355740796);
        /// // Returns: PlayerModel with all stats, inventory, and progression data
        /// </remarks>
        public static PlayerModel LoadByUserId(ulong userId)
        {
            // Construct the file path using Discord user ID
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            // Check if player file exists; return empty player if not found
            if (!File.Exists(path))
            {
                LogService.Error($"[PlayerDataManager.LoadByUserId] Player file not found for userId {userId}. Returning empty PlayerModel.");
                return new PlayerModel { Id = userId };
            }

            // Attempt to deserialize player from JSON
            var player = JsonDataManager.LoadObjectFromJson<PlayerModel>(path);

            // Return loaded player or fallback to empty player if deserialization fails
            return player ?? new PlayerModel { Id = userId };
        }

        /// <summary>
        /// Loads all registered player data from the player data directory.
        /// 
        /// Scans the "Data/Player" directory for all JSON files and deserializes them
        /// into a list of PlayerModel objects. Skips any files that fail to deserialize.
        /// </summary>
        /// <returns>
        /// A list of all PlayerModel objects found. Returns an empty list if the directory
        /// doesn't exist or no valid JSON files are present.
        /// </returns>
        /// <remarks>
        /// Directory: "Data/Player"
        /// Pattern: "*.json" files
        /// 
        /// This method is useful for:
        /// - Initializing the GameData collection with all players at startup
        /// - Validating that all player data is consistent
        /// - Generating statistics about all registered players
        /// 
        /// Example:
        /// var allPlayers = PlayerDataManager.LoadAll();
        /// // Returns: List of all PlayerModel objects
        /// </remarks>
        public static List<PlayerModel>? LoadAll()
        {
            // Define the player data directory path
            string directoryPath = "Data/Player";

            // Return empty list if directory doesn't exist
            if (!Directory.Exists(directoryPath))
                return new List<PlayerModel>();

            var players = new List<PlayerModel>();
            // Retrieve all JSON files from the directory
            var files = Directory.GetFiles(directoryPath, "*.json");

            // Deserialize each file and add valid players to the list
            foreach (var file in files)
            {
                var player = JsonDataManager.LoadObjectFromJson<PlayerModel>(file);
                if (player != null)
                {
                    players.Add(player);
                }
            }

            return players;
        }

        /// <summary>
        /// Generates a unique player name by appending a random number suffix if the base name already exists.
        /// 
        /// Checks existing player names case-insensitively. If the provided name is already taken,
        /// appends a random 4-digit suffix (e.g., "PlayerName#1234") until a unique name is found.
        /// </summary>
        /// <param name="baseName">The desired player name to use as the base (e.g., "Thorgrim").</param>
        /// <returns>
        /// Either the base name if it's unique, or a modified name with a random suffix.
        /// Guaranteed to be unique among all registered players.
        /// </returns>
        /// <remarks>
        /// Algorithm:
        /// 1. Load all existing players
        /// 2. Check if baseName already exists (case-insensitive comparison)
        /// 3. If unique, return baseName as-is
        /// 4. If taken, append random suffix: "{baseName}#{1000-9999}" and retry
        /// 5. Repeat until unique name is found
        /// 
        /// Examples:
        /// - "Gandalf" → "Gandalf" (if unique)
        /// - "Gandalf" → "Gandalf#7342" (if "Gandalf" already exists)
        /// - "Elara" → "Elara#4891" (if "Elara" already exists)
        /// </remarks>
        public static string GenerateUniquePlayerName(string baseName)
        {
            // Load all existing players to check name availability
            var players = LoadAll();
            var existingNames = players?.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

            string uniqueName = baseName;
            var rand = new Random();

            // Continue generating random suffixes until a unique name is found
            while (existingNames.Contains(uniqueName))
            {
                LogService.Error($"[PlayerDataManager.GenerateUniquePlayerName] Player Name {uniqueName} already exists. Creating unique player name...");

                // Generate new name with random 4-digit suffix
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
