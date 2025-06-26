using Adventure.Models.Enviroment;
using Adventure.Services;
using Adventure.Models.Player;
using Discord.Interactions;
using System.Collections.Concurrent;
using Discord;

namespace Adventure.Modules
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Store player progress using a thread-safe dictionary
        private static readonly ConcurrentDictionary<ulong, GameStateModel> playerStates = new();

        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task SlashCommandStartHandler()
        {
            // Defer the response to prevent the "No response" error
            await DeferAsync();

            LogService.SessionDivider('=', "START");
            LogService.Info("[AdventureGameModule.SlashCommandStartHandler]          > Slash Command /start is executed");


            if (!playerStates.ContainsKey(Context.User.Id))
                playerStates[Context.User.Id] = new GameStateModel();

            playerStates[Context.User.Id].Inventory.Clear(); // reset inventory
            playerStates[Context.User.Id].Inventory.Add("Item_1", 1);
            playerStates[Context.User.Id].Inventory.Add("Item_2", 2);
            playerStates[Context.User.Id].Inventory.Add("Item_3", 3);



            // Send a follow-up response after the processing is complete.
            await FollowupAsync("Slash Command /start is executed");
            //await FollowupAsync("Your adventure has begun!");
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

            

            LogService.Info("[AdventureGameModule.SlashCommandInventoryHandler]          > Slash Command /inventory is executed");
            await FollowupAsync($"Slash Command /inventory is executed...\n");

            EmbedBuilder embed = InventoryEmbedBuilder.BuildInventoryEmbed(gameState.Inventory);

            await FollowupAsync(embed: embed.Build());

        }
    }
}
