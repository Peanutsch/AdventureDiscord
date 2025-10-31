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
            var buttons = SlashCommandHelpers.BuildEncounterButtons();

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
        #endregion

        #region === Slashcommand "walk" ===
        [SlashCommand("walk", "Simulate walking through tiles with directional buttons.")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();

            // --- Get user Id ---
            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "SlashCommand: walk");
            LogService.Info($"[/walk] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            // --- Get/Create user profile ---
            var player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            // --- Check for last savepoint ---
            var tile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint);

            // --- Fallback to START tile if no (valid) savepoint ---
            if (tile == null)
            {
                LogService.Info($"[WalkCommand] Savepoint '{player.Savepoint}' invalid. Fallback to START tile.");
                tile = SlashCommandHelpers.FindStartTile();

                if (tile != null)
                {
                    // --- Update player's savepoint ---
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

            // --- Get name of area ---
            string startArea = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var area)
                ? area.Name
                : "Unknown Area";

            LogService.Info($"[/walk] Starting in area: {startArea}, position: {tile.TilePosition}");

            // --- Build Embed and buttons ---
            var embed = EmbedBuildersMap.EmbedWalk(tile);
            var components = ButtonBuildersMap.BuildDirectionButtons(tile);

            await FollowupAsync(embed: embed.Build(), components: components?.Build());
        }
        #endregion
    }
}