using Adventure.Models.Enviroment;
using Adventure.Services;
using Adventure.Models.Player;
using Discord.Interactions;
using System.Collections.Concurrent;
using Discord;
using Adventure.Events.EventService;
using Adventure.Quest.Battle;

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
            // Load inventory
            InventoryStateService.LoadInventory(Context.User.Id);

            await DeferAsync();

            var creature = EncounterService.CreatureRandomizer();

            if (creature == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                LogService.Error("[AdventureGameModule.SlashCommandEncounterHandler] > No creature could be picked.");
                return;
            }

            // Update BattleState 
            BattleEngine.SetCreature(Context.User.Id, creature);

            var embed = EncounterService.GetRandomEncounter(creature);

            var buttons = new ComponentBuilder()
                .WithButton("Attack", "btn_attack", ButtonStyle.Danger)
                .WithButton("Flee", "btn_flee", ButtonStyle.Secondary);

            // Reset step from GameEngine.HandleEncounterAction to [start]
            BattleEngine.SetStep(Context.User.Id, "start");

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
    }
}