using Adventure.Models.Enviroment;
using Adventure.Services;
using Adventure.Models.Player;
using Discord.Interactions;
using System.Collections.Concurrent;
using Discord;
using Adventure.Quest.Battle;
using Adventure.Loaders;
using Adventure.Data;
using Microsoft.VisualBasic;
using Adventure.Quest.Encounter;
using Adventure.Models.BattleState;

namespace Adventure.Modules
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
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

            var npc = EncounterService.NpcRandomizer();
            var state = BattleEngine.GetBattleState(Context.User.Id);
            if (npc == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                return;
            }

            SlashEncounterHelpers.SetupBattleState(user.Id, npc);

            var embed = EncounterService.BuildEmbedRandomEncounter(npc, state);
            var buttons = SlashEncounterHelpers.BuildEncounterButtons();

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
    }
}