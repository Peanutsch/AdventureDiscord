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
        /// Retrieves a TileModel from a savepoint.
        /// Supports:
        /// 1. Absolute TileId (areaId:row,col)
        /// 2. TileName like "START"
        /// </summary>
        public static TileModel? GetTileFromSavePoint(string savepoint)
        {
            if (string.IsNullOrWhiteSpace(savepoint))
                return null;

            // 1️⃣ Direct TileId lookup (preferred)
            if (TestHouseLoader.TileLookup.TryGetValue(savepoint, out var tile))
            {
                return tile;
            }

            // 2️⃣ Fallback: search TileName in all areas
            foreach (var area in TestHouseLoader.AreaLookup.Values)
            {
                tile = area.Tiles.FirstOrDefault(t =>
                    string.Equals(t.TileName, savepoint, StringComparison.OrdinalIgnoreCase));
                if (tile != null)
                    return tile;
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