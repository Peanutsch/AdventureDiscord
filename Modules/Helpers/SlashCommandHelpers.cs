using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Attributes;
using Adventure.Models.Map;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Modules.Helpers
{
    /// <summary>
    /// Provides helper methods for Discord slash command processing and execution.
    /// 
    /// This static class contains utility functions for:
    /// - Retrieving Discord users from interaction context
    /// - Loading or creating player accounts
    /// - Setting up battle state for encounters
    /// - Building UI components (buttons, embeds)
    /// - Retrieving player save points and map tiles
    /// 
    /// These helpers simplify common operations used across multiple slash command handlers,
    /// reducing code duplication and improving maintainability.
    /// 
    /// <remarks>
    /// Usage: Call static methods directly via SlashCommandHelpers.MethodName()
    /// 
    /// Common Workflow:
    /// 1. Get Discord user: GetDiscordUser()
    /// 2. Load/create player: GetOrCreatePlayer()
    /// 3. Setup battle: SetupBattleState()
    /// 4. Build UI: BuildEncounterButtons()
    /// </remarks>
    /// </summary>
    public static class SlashCommandHelpers
    {
        #region === Get Discord User ===

        /// <summary>
        /// Retrieves a Discord user object from the Discord client by their user ID.
        /// 
        /// Looks up the user in the client's user cache. This is useful for accessing
        /// user information (username, avatar, etc.) outside of the direct interaction context.
        /// </summary>
        /// <param name="context">The current Discord interaction context.</param>
        /// <param name="userId">The Discord user ID to look up.</param>
        /// <returns>
        /// The IUser object if found in the client's cache, otherwise null.
        /// </returns>
        /// <remarks>
        /// Note: This method only searches the client's cached users. If the user hasn't been
        /// cached yet, the method will return null. For guaranteed lookups, use the REST API.
        /// </remarks>
        public static IUser? GetDiscordUser(SocketInteractionContext context, ulong userId)
        {
            return context.Client.GetUser(userId);
        }

        #endregion

        #region === Get/Create Player ===

        /// <summary>
        /// Loads an existing player or creates a new one if no save data exists.
        /// 
        /// Attempts to load player data from disk. If successful, returns the loaded player.
        /// If not found or if deserialization fails, creates a new player with default attributes
        /// and starting equipment.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        /// <param name="playerName">The display name for the player (used if creating new player).</param>
        /// <returns>
        /// A PlayerModel representing either the loaded or newly created player.
        /// Never returns null.
        /// </returns>
        /// <remarks>
        /// File Check: "Data/Player/{userId}.json"
        /// 
        /// If File Exists: Loads from disk via PlayerDataManager.LoadByUserId()
        /// If File Missing: Creates new player via PlayerDataManager.CreateNewPlayer()
        /// 
        /// New players receive:
        /// - Default attributes (STR 10, DEX 14, CON 10, etc.)
        /// - Starting equipment (Short sword, Hide armor)
        /// - Maximum carry capacity
        /// - Starting savepoint location
        /// </remarks>
        public static PlayerModel GetOrCreatePlayer(ulong userId, string playerName)
        {
            // Construct file path for player data
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "Player", $"{userId}.json");

            // If player file doesn't exist, create a new player
            if (!File.Exists(path))
            {
                LogService.Error($"[SlashEncounterHelpers.GetOrCreatePlayer] No player file found. Create new file for {playerName} ({userId})");
                return PlayerDataManager.CreateNewPlayer(userId, playerName);
            }

            // Load existing player or create new if load fails
            //PlayerModel player = PlayerDataManager.LoadByUserId(userId);
            //return player ?? PlayerDataManager.CreateNewPlayer(userId, playerName);
            return PlayerDataManager.LoadByUserId(userId) ?? PlayerDataManager.CreateNewPlayer(userId, playerName);
        }

        #endregion

        #region === Load Inventory (Not in Use) ===
        /*
         * Temp setup method loading Inventory
        public static void EnsureInventoryLoaded(ulong userId)
        {
            if (GameData.Inventory == null)
            {
                LogService.Info("[SlashEncounterHelpers.EnsureInventoryLoaded] Inventory not loaded, reloading...");
                GameData.Inventory = InventoryLoader.Load();
            }

            InventoryStateService.LoadInventory(userId);
        }
        */
        #endregion

        #region === Setup Battle State ===

        /// <summary>
        /// Initializes the battle state for an encounter between a player and an NPC.
        /// 
        /// Sets up both player and NPC combat data, initializes the battle step tracker,
        /// and prepares the battle system for the encounter.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player entering battle.</param>
        /// <param name="npc">The NpcModel representing the enemy to fight.</param>
        /// <remarks>
        /// This method performs:
        /// 1. NPC setup via NpcSetup.SetupNpc() - configures enemy stats and equipment
        /// 2. Battle step initialization via EncounterBattleStepsSetup.SetStep() - sets initial battle phase
        /// 
        /// After this call, the battle is ready for player actions like weapon selection and attacks.
        /// </remarks>
        public static void SetupBattleState(ulong userId, NpcModel npc)
        {
            // Configure NPC combat properties and initial state
            NpcSetup.SetupNpc(userId, npc);

            // Initialize battle to the starting phase
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.Start);
        }

        /// <summary>
        /// Builds and returns a component builder with standard battle encounter buttons.
        /// 
        /// Creates a button row containing "Attack" and "Flee" actions for the player.
        /// These buttons allow the player to choose their action during an encounter.
        /// </summary>
        /// <param name="userId">The Discord user ID (used for button IDs to ensure user-specific handling).</param>
        /// <returns>
        /// A ComponentBuilder with two buttons: "Attack" (red, dangerous) and "Flee" (gray, secondary).
        /// </returns>
        /// <remarks>
        /// Button Layout:
        /// [Attack]  [Flee]
        /// 
        /// Button IDs:
        /// - "btn_attack" - Triggers attack action for the current player
        /// - "battle_flee_{userId}" - Triggers flee action specific to this player
        /// 
        /// Usage:
        /// var buttons = BuildEncounterButtons(userId);
        /// await context.FollowupAsync(embed: embed.Build(), components: buttons.Build());
        /// </remarks>
        public static ComponentBuilder BuildEncounterButtons(ulong userId)
        {
            return new ComponentBuilder()
                .WithButton("Attack", "btn_attack", ButtonStyle.Danger)
                .WithButton("Flee", $"battle_flee_{userId}", ButtonStyle.Secondary);
        }

        #endregion

        #region === Get Tile Savepoint ===

        /// <summary>
        /// Retrieves a TileModel from the map using a savepoint identifier.
        /// 
        /// Supports two lookup methods:
        /// 1. Direct TileId lookup (preferred): "areaId:row,col" format
        /// 2. Fallback TileName search: Searches all areas for matching TileName
        /// 
        /// This method handles various savepoint formats flexibly to ensure players
        /// can always find their last known location.
        /// </summary>
        /// <param name="savepoint">
        /// The savepoint identifier. Can be:
        /// - TileId (direct lookup, e.g., "area_tavern:5,3")
        /// - TileName (fallback search, e.g., "START", "BOSS_ROOM", etc.)
        /// </param>
        /// <returns>
        /// The found TileModel if a match is discovered, otherwise null.
        /// If null is returned, the player cannot load their last position.
        /// </returns>
        /// <remarks>
        /// Lookup Priority:
        /// 1. Direct TileId lookup in TestHouseLoader.TileLookup (fastest)
        /// 2. Fallback: Search all areas for matching TileName (case-insensitive)
        /// 3. If not found: Return null
        /// 
        /// Example Savepoints:
        /// - "area_tavern:5,3" → Found via direct TileId lookup
        /// - "START" → Found via TileName search in all areas
        /// - "BOSS_ROOM" → Found via TileName search in all areas
        /// 
        /// Usage:
        /// TileModel tile = GetTileFromSavePoint(player.Savepoint);
        /// if (tile != null)
        ///     await DisplayTile(tile);
        /// else
        ///     await DisplayDefaultLocation(); // Fallback to starting area
        /// </remarks>
        public static TileModel? GetTileFromSavePoint(string savepoint)
        {
            LogService.Info($"[SlashCommandHelpers.GetTileFromSavePoint] Last updated savepoint {savepoint}");

            if (string.IsNullOrWhiteSpace(savepoint))
                return null;

            // Direct TileId lookup
            if (TestHouseLoader.TileLookup.TryGetValue(savepoint, out TileModel? tile))
            {
                LogService.Info($"[SlashCommandHelpers.GetTileFromSavePoint] Found savepoint {savepoint} on map...");
                return tile;
            }

            // Fallback: search TileName in all areas
            foreach (TestHouseAreaModel area in TestHouseLoader.AreaLookup.Values)
            {
                tile = area.Tiles.FirstOrDefault(t =>
                    string.Equals(t.TileName, savepoint, StringComparison.OrdinalIgnoreCase));
                if (tile != null)
                {
                    LogService.Info($"Found savepoint {tile.TileId} on map...");
                    return tile;
                }
            }

            LogService.Error($"[TileHelpers.GetTileFromSavePoint] No tile found for savepoint '{savepoint}'.");
            return null;
        }

        /// <summary>
        /// Finds the START tile in all areas and returns its TileId and TilePosition.
        /// </summary>
        public static TileModel? FindStartTile()
        {
            foreach (TestHouseAreaModel area in TestHouseLoader.AreaLookup.Values)
            {
                // Search for a tile with TileName "START" (case-insensitive)
                TileModel? startTile = area.Tiles.FirstOrDefault(t => t.TileName.Equals("START", StringComparison.OrdinalIgnoreCase));
                if (startTile != null)
                {
                    // If TilePosition is missing or invalid, find it in the layout
                    if (string.IsNullOrWhiteSpace(startTile.TilePosition) || startTile.TilePosition == "ERROR_TILE_POSITION")
                    {
                        // Search the area's layout for the "START" tile to determine its position
                        for (int row = 0; row < area.Layout.Count; row++)
                        {
                            for (int col = 0; col < area.Layout[row].Count; col++)
                            {
                                if (area.Layout[row][col].Equals("START", StringComparison.OrdinalIgnoreCase))
                                {
                                    startTile.TilePosition = $"{row},{col}";
                                    break;
                                }
                            }
                        }
                    }

                    return startTile;
                }
            }

            LogService.Error("[PlayerStartHelper.FindStartTile] No START tile found in any area!");
            return null;
        }
        #endregion

        #region === Adventure Command Helpers ===

        /// <summary>
        /// Validates that the player does not have an active adventure or battle session.
        /// If a session is stuck (> 5 minutes old), auto-cleanup and allow new session.
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        /// <returns>True if player can start a new session; false if actively in one.</returns>
        public static (bool IsValid, string? ErrorMessage) ValidateNoActiveSession(ulong userId)
        {
            PlayerModel? player = GetOrCreatePlayer(userId, "");
            if (player == null)
                return (false, "⚠️ Error loading player data.");

            if (player.CurrentState != PlayerState.Idle)
            {
                TimeSpan inactivityTime = DateTime.UtcNow - player.LastActivityTime;
                const int INACTIVITY_TIMEOUT_MINUTES = 5;

                // Auto-cleanup if session is stuck (> 5 minutes old)
                if (inactivityTime > TimeSpan.FromMinutes(INACTIVITY_TIMEOUT_MINUTES))
                {
                    LogService.Info($"[SlashCommandHelpers.ValidateNoActiveSession] Player {userId} session stuck (inactive {inactivityTime.TotalMinutes:F1}min). Auto-cleanup.");
                    player.CurrentState = PlayerState.Idle;
                    JsonDataManager.UpdatePlayerState(userId, PlayerState.Idle);
                    return (true, null);  // Allow new session
                }

                // Session is recent, block it
                string sessionType = player.CurrentState == PlayerState.InAdventure ? "adventure" : "battle";
                LogService.Info($"[SlashCommandHelpers.ValidateNoActiveSession] Player {userId} attempted to start adventure while in {player.CurrentState} state.");
                return (false, $"⚠️ You already have an active {sessionType} session.");
            }

            return (true, null);
        }

        /// <summary>
        /// Initializes or retrieves the player profile.
        /// </summary>
        /// <param name="user">The Discord user.</param>
        /// <returns>The player model, or null if initialization fails.</returns>
        public static PlayerModel? InitializePlayer(IUser user)
        {
            PlayerModel player = GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
                return null;

            return player;
        }

        /// <summary>
        /// Gets or creates a player for the stats command.
        /// </summary>
        /// <param name="user">The Discord user.</param>
        /// <returns>The player model, or null if retrieval fails.</returns>
        public static PlayerModel? GetPlayerForStats(IUser user)
        {
            PlayerModel? player = GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
                return null;

            LogService.Info($"[SlashCommandHelpers.GetPlayerForStats] Retrieved player stats for {user.GlobalName ?? user.Username}");
            return player;
        }

        /// <summary>
        /// Determines the current tile for the player.
        /// Uses savepoint if valid, otherwise falls back to START tile.
        /// </summary>
        /// <param name="player">The player profile.</param>
        /// <param name="userId">The Discord user ID for updating savepoint.</param>
        /// <returns>Tuple with tile and error message (tile null if error).</returns>
        public static (TileModel? tile, string? errorMessage) DetermineTile(PlayerModel player, ulong userId)
        {
            // Try to get tile from savepoint
            TileModel? tile = GetTileFromSavePoint(player.Savepoint);

            // Fallback to START tile if savepoint invalid
            if (tile == null)
            {
                LogService.Info($"[SlashCommandHelpers.DetermineTile] Savepoint '{player.Savepoint}' invalid. Fallback to START tile.");
                tile = FindStartTile();

                if (tile == null)
                    return (null, "❌ No START tile found in any area. Cannot start.");

                // Update player's savepoint to START tile
                player.Savepoint = $"{tile.AreaId}:{tile.TilePosition}";
                JsonDataManager.UpdatePlayerSavepoint(userId, player.Savepoint);
                LogService.Info($"[SlashCommandHelpers.DetermineTile] Position saved as new savepoint: {player.Savepoint}");
            }

            return (tile, null);
        }

        /// <summary>
        /// Builds a stats embed for the given player.
        /// </summary>
        /// <param name="player">The player to display stats for.</param>
        /// <returns>An embed containing the player's stats.</returns>
        public static Embed BuildStatsEmbed(PlayerModel player)
        {
            return new EmbedBuilder()
                .WithTitle($"{player.Name}'s Stats")
                .WithColor(Color.Green)
                .AddField("Level", player.Level, true)
                .AddField("HP", $"{player.Hitpoints}", true)
                .AddField("Experience", $"{player.XP}", true)
                .AddField("Strength", player.Attributes.Strength, true)
                .AddField("Dexterity", player.Attributes.Dexterity, true)
                .AddField("Constitution", player.Attributes.Constitution, true)
                .AddField("Intelligence", player.Attributes.Intelligence, true)
                .AddField("Wisdom", player.Attributes.Wisdom, true)
                .AddField("Charisma", player.Attributes.Charisma, true)
                //.AddField("Gold", player.Gold, true)
                .WithFooter("Keep adventuring to improve your stats!")
                .Build();
        }

        /// <summary>
        /// Sets the player's state to InAdventure and updates activity time in JSON.
        /// </summary>
        /// <param name="player">The player to update.</param>
        /// <param name="userId">The Discord user ID.</param>
        public static void SetPlayerStateInAdventure(PlayerModel player, ulong userId)
        {
            player.CurrentState = PlayerState.InAdventure;
            player.LastActivityTime = DateTime.UtcNow;
            JsonDataManager.UpdatePlayerState(userId, PlayerState.InAdventure);
            JsonDataManager.UpdatePlayerLastActivityTime(userId);
            LogService.Info($"[SlashCommandHelpers.SetPlayerStateInAdventure] Player {userId} state set to InAdventure, activity time updated.");
        }

        #endregion

        #region === Ability Score Improvements ===

        /// <summary>
        /// Checks if a player is eligible for an Ability Score Improvement at their current level.
        /// Players are eligible at levels: 4, 8, 12, 16, 19 (D&D 5e milestones).
        /// </summary>
        public static bool CheckAbilityScoreEligibility(PlayerModel player)
        {
            int[] asiLevels = LevelHelpers.LevelMilestones;
            return Array.Exists(asiLevels, level => level == player.Level);
        }

        /// <summary>
        /// Builds an embed and button components for the Ability Score Improvement selection screen.
        /// Shows current ability scores and 6 buttons (one for each attribute).
        /// </summary>
        public static (EmbedBuilder embed, ComponentBuilder components) BuildAbilityScoreImprovementEmbed(PlayerModel player)
        {
            var embed = new EmbedBuilder()
                .WithTitle("⭐ Ability Score Improvement")
                .WithDescription($"Congratulations **{player.Name}**! You've reached level {player.Level} and are eligible for an Ability Score Improvement!")
                .WithColor(Color.Purple)
                .AddField("📊 Current Scores",
                    $"STR: {player.Attributes.Strength} | DEX: {player.Attributes.Dexterity} | CON: {player.Attributes.Constitution}\n" +
                    $"INT: {player.Attributes.Intelligence} | WIS: {player.Attributes.Wisdom} | CHA: {player.Attributes.Charisma}", false)
                .AddField("🎁 Your Reward", "Choose ONE attribute to increase by +2", false)
                .WithFooter("Click a button below to apply your improvement");

            var components = new ComponentBuilder()
                .WithButton("Strength +2", $"asi_str_{player.Id}", ButtonStyle.Primary)
                .WithButton("Dexterity +2", $"asi_dex_{player.Id}", ButtonStyle.Primary)
                .WithButton("Constitution +2", $"asi_con_{player.Id}", ButtonStyle.Primary, row: 1)
                .WithButton("Intelligence +2", $"asi_int_{player.Id}", ButtonStyle.Primary, row: 1)
                .WithButton("Wisdom +2", $"asi_wis_{player.Id}", ButtonStyle.Primary, row: 1)
                .WithButton("Charisma +2", $"asi_cha_{player.Id}", ButtonStyle.Primary, row: 2);

            return (embed, components);
        }

        /// <summary>
        /// Applies a +2 ability score improvement to the specified attribute and persists to JSON.
        /// </summary>
        public static (bool Success, string Message) ApplyAbilityScoreImprovement(
            PlayerModel player,
            ulong userId,
            string attribute)
        {
            // 1. Validate eligibility
            if (!CheckAbilityScoreEligibility(player))
                return (false, "❌ You are not eligible for an ability score improvement at this level.");

            // 2. Apply the improvement
            try
            {
                switch (attribute.ToLower())
                {
                    case "strength" or "str":
                        player.Attributes.Strength += 2;
                        break;
                    case "dexterity" or "dex":
                        player.Attributes.Dexterity += 2;
                        break;
                    case "constitution" or "con":
                        player.Attributes.Constitution += 2;
                        break;
                    case "intelligence" or "int":
                        player.Attributes.Intelligence += 2;
                        break;
                    case "wisdom" or "wis":
                        player.Attributes.Wisdom += 2;
                        break;
                    case "charisma" or "cha":
                        player.Attributes.Charisma += 2;
                        break;
                    default:
                        return (false, $"❌ Unknown attribute: {attribute}");
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"[SlashCommandHelpers.ApplyAbilityScoreImprovement] Error: {ex.Message}");
                return (false, "❌ An error occurred while applying the improvement.");
            }

            // 3. Increment ASI counter
            player.ASIs++;

            // 4. Persist to JSON
            try
            {
                JsonDataManager.SaveNewPlayerToJson(userId, player);
                LogService.Info($"[SlashCommandHelpers.ApplyAbilityScoreImprovement] Applied +2 {attribute} for player {userId}. Total ASIs: {player.ASIs}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[SlashCommandHelpers.ApplyAbilityScoreImprovement] Failed to persist: {ex.Message}");
                return (false, "⚠️ Improvement applied locally, but failed to save.");
            }

            int newScore = GetAttributeValue(player.Attributes, attribute);
            return (true, $"✅ +2 {attribute.ToUpper()} applied! Your new score is {newScore}");
        }

        /// <summary>
        /// Helper to get the current value of an attribute.
        /// </summary>
        private static int GetAttributeValue(AttributesModel attributes, string attribute)
        {
            return attribute.ToLower() switch
            {
                "strength" or "str" => attributes.Strength,
                "dexterity" or "dex" => attributes.Dexterity,
                "constitution" or "con" => attributes.Constitution,
                "intelligence" or "int" => attributes.Intelligence,
                "wisdom" or "wis" => attributes.Wisdom,
                "charisma" or "cha" => attributes.Charisma,
                _ => 0
            };
        }

        /// <summary>
        /// Sends ASI options to player's DM if they are eligible at their current level.
        /// Automatically triggered when a player levels up to a milestone.
        /// </summary>
        public static async Task<bool> SendAbilityScoreImprovementIfEligibleAsync(ulong userId, SocketInteraction interaction)
        {
            try
            {
                LogService.Info($"[SendAbilityScoreImprovementIfEligibleAsync] Checking ASI for player {userId}");

                // Load player
                PlayerModel? player = GetOrCreatePlayer(userId, "");
                if (player == null)
                {
                    LogService.Error($"[SendAbilityScoreImprovementIfEligibleAsync] Failed to load player {userId}");
                    return false;
                }

                // Check eligibility
                if (!CheckAbilityScoreEligibility(player))
                {
                    LogService.Info($"[SendAbilityScoreImprovementIfEligibleAsync] Player {userId} not eligible at level {player.Level}");
                    return false;
                }

                LogService.Info($"[SendAbilityScoreImprovementIfEligibleAsync] Player {userId} IS eligible at level {player.Level}! Sending ASI options...");

                // Build ASI embed and buttons
                var (embed, components) = BuildAbilityScoreImprovementEmbed(player);

                // Send to DM
                IUserMessage? dmMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                    interaction,
                    embed.Build(),
                    components.Build());

                if (dmMessage != null)
                {
                    // Track message ID for button state management
                    BattlePrivateMessageHelper.SetActiveBattleMessage(userId, dmMessage.Id);
                    LogService.Info($"[SendAbilityScoreImprovementIfEligibleAsync] ✅ ASI options sent to DM for player {player.Name} at level {player.Level}");
                    return true;
                }

                LogService.Error($"[SendAbilityScoreImprovementIfEligibleAsync] Failed to send ASI message to DM for player {userId}");
                return false;
            }
            catch (Exception ex)
            {
                LogService.Error($"[SendAbilityScoreImprovementIfEligibleAsync] Exception: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}