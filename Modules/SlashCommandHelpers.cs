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
    public static class SlashCommandHelpers
    {
        #region === Get Discord User ===
        public static IUser? GetDiscordUser(SocketInteractionContext context, ulong userId)
        {
            return context.Client.GetUser(userId);
        }
        #endregion

        #region === Get/Create Player ===
        public static PlayerModel GetOrCreatePlayer(ulong userId, string playerName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[SlashEncounterHelpers.GetOrCreatePlayer] No player file found. Creating for {playerName} ({userId})");
                return PlayerDataManager.CreateNewPlayer(userId, playerName);
            }

            var player = PlayerDataManager.LoadByUserId(userId);
            return player ?? PlayerDataManager.CreateNewPlayer(userId, playerName);
        }
        #endregion

        #region === Load Inventory ===
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

        #region === Setup Battlestate ===
        public static void SetupBattleState(ulong userId, NpcModel npc)
        {
            NpcSetup.SetupNpc(userId, npc);
            EncounterBattleStepsSetup.SetStep(userId, "start");
        }

        public static ComponentBuilder BuildEncounterButtons()
        {
            return new ComponentBuilder()
                .WithButton("Attack", "btn_attack", ButtonStyle.Danger)
                .WithButton("Flee", "btn_flee", ButtonStyle.Secondary);
        }
        #endregion

        #region === Get Tile Savepoint ===
        /// <summary>
        /// Finds the tile corresponding to the player's savepoint.
        /// Order: Direct key > TileId > fallback START 
        /// </summary>
        public static TileModel? GetTileFromSavePoint(string? savepoint)
        {
            LogService.Info($"[SlashEncounterHelpers.GetTileFromSavePoint] Searching for savepoint {savepoint}...");
            if (string.IsNullOrWhiteSpace(savepoint))
            {
                LogService.Error("[SlashEncounterHelpers.GetTileFromSavePoint] Savepoint was null or empty — fallback START tile.");
                return FindStartTile();
            }

            if (savepoint.Equals("START", StringComparison.OrdinalIgnoreCase))
            {
                LogService.Info("[GetTileFromSavePoint] Savepoint is START — loading start tile directly.");
                return FindStartTile();
            }

            // Direct key match (e.g., "MainRoom:3,2")
            if (MainHouseLoader.TileLookup.TryGetValue(savepoint, out var foundTile))
            {
                LogService.Info($"[SlashEncounterHelpers.GetTileFromSavePoint] Found tile via direct key match: '{savepoint}'.");
                return foundTile;
            }

            // Search by TileId (e.g., "tile_3_2")
            var tileById = MainHouseLoader.TileLookup.Values
                .FirstOrDefault(t =>
                    t.TileId.Equals(savepoint, StringComparison.OrdinalIgnoreCase));

            if (tileById != null)
            {
                LogService.Info($"[SlashEncounterHelpers.GetTileFromSavePoint] Found tile via TileId match: '{tileById.TileId}'.");
                return tileById;
            }

            // Use START as fallback
            LogService.Error($"[SlashEncounterHelpers.GetTileFromSavePoint] No tile found for savepoint '{savepoint}'. Falling back to START tile.");
            return FindStartTile();
        }

        /// <summary>
        /// Finds the first tile that contains 'START' in its TileGrid.
        /// </summary>
        private static TileModel? FindStartTile()
        {
            var startTile = MainHouseLoader.AllTiles
                .FirstOrDefault(t => t.TileGrid?
                .Any(row => row.Any(cell =>
                    cell.Equals("START", StringComparison.OrdinalIgnoreCase))) == true);

            if (startTile != null)
                LogService.Info($"[SlashEncounterHelpers.FindStartTile] Found START tile: {startTile.TilePosition}");
            else
                LogService.Error("[SlashEncounterHelpers.FindStartTile] No START tile found!");

            return startTile;
        }
        #endregion
    }
}
