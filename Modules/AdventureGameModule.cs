using Adventure.Models.Map;
using Adventure.Services;
using Adventure.Models.Player;
using Discord.Interactions;
using System.Collections.Concurrent;
using Discord;
using Adventure.Loaders;
using Adventure.Data;
using Microsoft.VisualBasic;
using Adventure.Quest.Encounter;
using Adventure.Models.BattleState;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Map;

namespace Adventure.Modules
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        #region === Slashcommand "start" ===
        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task SlashCommandStartHandler()
        {
            var user = Context.Client.GetUser(Context.User.Id);
            if (user != null)
            {
                string displayName = user.GlobalName ?? user.Username;
                LogService.Info($"[Encounter] Discord user '{displayName}' (ID: {user.Id})");
            }

            // Defer the response to prevent the "No response" error
            await DeferAsync();

            LogService.SessionDivider('=', "START");
            LogService.Info("[AdventureGameModule.SlashCommandStartHandler] > Slash Command /start is executed");

            // Reset inventory to basic inventory: Shordsword and Dagger
            //InventoryStateService.LoadInventory(Context.User.Id);

            // Send a follow-up response after the processing is complete.
            //await FollowupAsync("Slash Command /start is executed");
            await FollowupAsync("Your adventure has begun!");
        }
        #endregion

        #region === Slashcommand "encounter" ===
        // Trigger encounter for testing
        [SlashCommand("encounter", "Triggers a random encounter")]
        public async Task SlashCommandEncounterHandler()
        {
            await DeferAsync();

            var user = SlashEncounterHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: Encounter");
            LogService.Info($"[/Encounter] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            var player = SlashEncounterHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            var npc = EncounterRandomizer.NpcRandomizer();

            if (npc == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                return;
            }

            SlashEncounterHelpers.SetupBattleState(user.Id, npc);

            var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            var buttons = SlashEncounterHelpers.BuildEncounterButtons();

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
        #endregion

        #region === Slashcommand "walk" ===
        /// <summary>
        /// Handles the /walk command, showing the player's current tile and available directions.
        /// Uses preloaded tiles from MergeAllRooms so TileText and TileGrid are preserved.
        /// </summary>
        [SlashCommand("walk", "Simulate walking over tiles with direction buttons.")]
        public async Task SlashCommandWalkHandler()
        {
            // Defer the response to avoid Discord "No response" errors
            await DeferAsync();

            try
            {
                // --- Get the Discord user ---
                var user = SlashEncounterHelpers.GetDiscordUser(Context, Context.User.Id);
                if (user == null)
                {
                    await FollowupAsync("⚠️ Error loading user data.");
                    return;
                }

                LogService.DividerParts(1, "Slashcommand: walk");
                LogService.Info($"[/walk] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

                // --- Check that map data exists ---
                if (GameData.TestHouse == null || GameData.TestHouse.Rooms.Count == 0)
                {
                    LogService.Error("[/walk] TestHouse map data missing!");
                    await FollowupAsync("⚠️ Map data is missing.");
                    return;
                }

                // --- Build TileLookup with precomputed TileModel objects ---
                var tileLookup = new Dictionary<string, TileModel>();

                foreach (var kvp in GameData.TestHouse.Rooms)
                {
                    string roomKey = kvp.Key;
                    var room = kvp.Value;

                    for (int r = 0; r < room.Layout.Count; r++)
                    {
                        for (int c = 0; c < room.Layout[r].Count; c++)
                        {
                            string pos = $"{r + 1},{c + 1}";
                            string cell = room.Layout[r][c];

                            // If this is the START tile, assign full room layout
                            var tileGrid = cell == "START"
                                ? room.Layout.Select(row => row.ToList()).ToList() // full room for START
                                : new List<List<string>> { new List<string> { cell } };

                            var tile = new TileModel
                            {
                                TileId = $"tile_{roomKey}_{r + 1}_{c + 1}",
                                TileName = $"Tile {roomKey} {r + 1},{c + 1}",
                                TilePosition = pos,
                                TileGrid = tileGrid,
                                Overlays = new List<string> { cell },
                                MapHeight = room.Layout.Count,
                                MapWidth = room.Layout[0].Count,
                                TileText = cell == "START" ? room.Description : $"Nothing to see on this tile ({cell})"
                            };

                            tileLookup[pos] = tile;
                        }
                    }
                }

                // --- Precompute exits for each tile ---
                foreach (var tile in tileLookup.Values)
                {
                    tile.TileExits = new Dictionary<string, string>();

                    var parts = tile.TilePosition.Split(',');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col))
                        continue;

                    var directions = new (int dr, int dc, string dir)[]
                    {
                        (-1, 0, "North"),
                        (1, 0, "South"),
                        (0, -1, "West"),
                        (0, 1, "East")
                    };

                    foreach (var (dr, dc, dir) in directions)
                    {
                        int newRow = row + dr;
                        int newCol = col + dc;
                        string newPos = $"{newRow},{newCol}";

                        if (tileLookup.TryGetValue(newPos, out var neighborTile))
                        {
                            string neighborType = neighborTile.TileGrid?[0][0] ?? "";
                            if (neighborType == "Floor" || neighborType == "Door" || neighborType == "START")
                            {
                                tile.TileExits[dir] = neighborTile.TileId;
                            }
                        }
                    }
                }

                // --- Find the START tile ---
                var startTile = tileLookup.Values.FirstOrDefault(t => t.Overlays?.Contains("START") == true);
                if (startTile == null)
                {
                    LogService.Error("[/walk] START tile not found!");
                    await FollowupAsync("⚠️ START tile missing in map data.");
                    return;
                }

                // --- Build the embed and direction buttons ---
                var embed = EmbedBuildersWalk.EmbedWalk(startTile);
                var components = EmbedBuildersWalk.BuildDirectionButtons(startTile);

                // --- Send the follow-up message ---
                await FollowupAsync(embed: embed.Build(), components: components.Build());
            }
            catch (Exception ex)
            {
                LogService.Error($"[/walk] Exception: {ex}");
                await FollowupAsync("⚠️ An error occurred during walking.");
            }
        }
        #endregion

        /*
        /// <summary>
        /// Walks the player to a specific map tile using its ID.
        /// </summary>
        [SlashCommand("walk", "Simulate walk over tiles with direction buttons...")]
        public async Task SlashCommandWalkHandler()
        {
            LogService.Info("Slashcommand /walk triggert...");

            await DeferAsync();

            var user = SlashEncounterHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: walk");
            LogService.Info($"[/walk] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            var startingPoint = GameData.Maps?
                                   .FirstOrDefault(tile => tile.TileGrid
                                   .Any(row => row.Contains("START")))?
                                   .TileId ?? "tile_start"; // fallback to tile_p

            LogService.Info($"[/walk] mapStartingpoint: {startingPoint}");

            var tileLookup = GameData.Maps?.ToDictionary(t => t.TilePosition, t => t);

            var map = GameData.Maps?.FirstOrDefault(m => m.TileName.Equals(startingPoint, StringComparison.OrdinalIgnoreCase));
            if (map == null)
            {
                LogService.Error($"[SlashCommandWalkHandler] Map '{startingPoint}' not found...");

                //await RespondAsync($"❌ Map '{startingPoint}' not found.", ephemeral: true);
                await FollowupAsync($"❌ Map '{startingPoint}' not found.", ephemeral: true);
                return;
            }

            var embed = EmbedBuildersWalk.EmbedWalk(map);
            var components = EmbedBuildersWalk.BuildDirectionButtons(map);

            LogService.Info("[SlashCommandWalkHandler] Sending embed + components...");
            LogService.Info($"Components: {components?.ToString() ?? "null"}");
            await FollowupAsync(embed: embed.Build(), components: components?.Build());
        }
        */
    }
}
