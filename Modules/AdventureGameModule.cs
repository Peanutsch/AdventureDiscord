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
        #endregion
    }
}