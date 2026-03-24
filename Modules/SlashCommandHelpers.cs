using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Modules
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
            var player = PlayerDataManager.LoadByUserId(userId);
            return player ?? PlayerDataManager.CreateNewPlayer(userId, playerName);
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
            EncounterBattleStepsSetup.SetStep(userId, "start");
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
        /// var tile = GetTileFromSavePoint(player.Savepoint);
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

            // 1️⃣ Direct TileId lookup (preferred)
            if (TestHouseLoader.TileLookup.TryGetValue(savepoint, out var tile))
            {
                LogService.Info($"[SlashCommandHelpers.GetTileFromSavePoint] Found savepoint {savepoint} on map...");
                return tile;
            }

            // 2️⃣ Fallback: search TileName in all areas
            foreach (var area in TestHouseLoader.AreaLookup.Values)
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
            foreach (var area in TestHouseLoader.AreaLookup.Values)
            {
                // Zoek een tile met type "START"
                var startTile = area.Tiles.FirstOrDefault(t => t.TileName.Equals("START", StringComparison.OrdinalIgnoreCase));
                if (startTile != null)
                {
                    // Zorg dat TilePosition correct is ingesteld (row,col)
                    if (string.IsNullOrWhiteSpace(startTile.TilePosition) || startTile.TilePosition == "ERROR_TILE_POSITION")
                    {
                        // Vind de positie in de layout
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
    }
}