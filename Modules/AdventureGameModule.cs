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

namespace Adventure.Modules
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Store player progress using a thread-safe dictionary
        private static readonly ConcurrentDictionary<ulong, InventoryStateModel> playerStates = new();

        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task SlashCommandStartHandler()
        {
            var user = Context.Client.GetUser(Context.User.Id); // Of gewoon: Context.User
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
            InventoryStateService.LoadInventory(Context.User.Id);

            // Send a follow-up response after the processing is complete.
            //await FollowupAsync("Slash Command /start is executed");
            await FollowupAsync("Your adventure has begun!");
        }

        [SlashCommand("inventory", "Show your inventory")]
        public async Task SlashCommandInventoryHandler()
        {
            await DeferAsync();

            if (!playerStates.TryGetValue(Context.User.Id, out var gameState))
            {
                await FollowupAsync("You haven't started your adventure yet. Use /start.");
                return;
            }

            LogService.Info("[AdventureGameModule.SlashCommandInventoryHandler] > Slash Command /inventory is executed");

            EmbedBuilder embed = InventoryEmbedBuilder.BuildInventoryEmbed(gameState.Inventory);

            await FollowupAsync(embed: embed.Build());

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

            SlashEncounterHelpers.EnsureInventoryLoaded(user.Id);

            var creature = EncounterService.CreatureRandomizer();
            if (creature == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                return;
            }

            SlashEncounterHelpers.SetupBattleState(user.Id, creature);

            var embed = EncounterService.BuildEmbedRandomEncounter(creature);
            var buttons = SlashEncounterHelpers.BuildEncounterButtons();

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
    }
}