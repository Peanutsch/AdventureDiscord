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

            var user = SlashEncounterHelpers.GetDiscordUser(Context ,Context.User.Id);
            if (user == null)
            {
                await RespondAsync("⚠️ Error loading user data.");
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
            var mapStartingpoint = "tile_a";

            var map = GameData.Maps?.FirstOrDefault(m => m.TileName.Equals(mapStartingpoint, StringComparison.OrdinalIgnoreCase));
            if (map == null)
            {
                LogService.Error($"[SlashCommandWalkHandler] Map '{mapStartingpoint}' not found...");

                await RespondAsync($"❌ Map '{mapStartingpoint}' not found.", ephemeral: true);
                return;
            }

            var embed = EmbedBuildersWalk.EmbedWalk(map);
            var components = EmbedBuildersWalk.BuildDirectionButtons(map);

            await RespondAsync(embed: embed.Build(), components: components?.Build());
        }
        #endregion
    }
}