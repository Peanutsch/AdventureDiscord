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
using static Adventure.Quest.Battle.Randomizers.EncounterRandomizer;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Map;

namespace Adventure.Modules
{
    public class SlashCommandModule : InteractionModuleBase<SocketInteractionContext>
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
                string username = user.Username;
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

            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await RespondAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: Encounter");
            LogService.Info($"[/Encounter] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            var player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
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

            SlashCommandHelpers.SetupBattleState(user.Id, npc);

            var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            var buttons = SlashCommandHelpers.BuildEncounterButtons(user.Id);

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
        #endregion

        #region === Slashcommand "walk" ===
        /// <summary>
        /// Handles the /walk slash command. Moves the player to the current tile, 
        /// toggles any lock if the tile has a switch, and builds the embed and directional buttons.
        /// </summary>
        [SlashCommand("walk", "Simulate walking through tiles with directional buttons.")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();

            // --- Get Discord user object ---
            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "SlashCommand: walk");
            LogService.Info($"[/walk] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            // --- Get or create player profile ---
            var player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            // --- Determine current tile from player's savepoint ---
            var tile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint);

            // --- Fallback to START tile if savepoint is invalid ---
            if (tile == null)
            {
                LogService.Info($"[WalkCommand] Savepoint '{player.Savepoint}' invalid. Fallback to START tile.");
                tile = SlashCommandHelpers.FindStartTile();

                if (tile != null)
                {
                    // Update player's savepoint to START tile
                    player.Savepoint = $"{tile.AreaId}:{tile.TilePosition}";
                    JsonDataManager.UpdatePlayerSavepoint(Context.User.Id, player.Savepoint);
                    LogService.Info($"[WalkCommand] Position saved as new savepoint: {player.Savepoint}");
                }
                else
                {
                    await FollowupAsync("❌ No START tile found in any area. Cannot start.", ephemeral: true);
                    return;
                }
            }

            // --- Toggle lock if the current tile has a switch ---
            if (TestHouseLockService.ToggleLockBySwitch(tile, TestHouseLoader.LockLookup))
            {
                LogService.Info($"[WalkCommand] Tile {tile.TileId} switch toggled lock: {tile.LockId}");
            }

            // --- Get name of area for logging ---
            string startArea = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var area)
                ? area.Name
                : "Unknown Area";
            LogService.Info($"[/walk] Starting in area: {startArea}, position: {tile.TilePosition}");

            // --- Build embed and directional buttons ---
            var embed = EmbedBuildersMap.EmbedWalk(tile); // Reads current tile state (including lock)
            var components = ButtonBuildersMap.BuildDirectionButtons(tile);

            // --- Send embed and buttons to Discord ---
            await FollowupAsync(embed: embed.Build(), components: components?.Build());
        }
        #endregion
    }
}