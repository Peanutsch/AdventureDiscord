using Adventure.Gateway;
using Adventure.Models.Enviroment;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Diagnostics;

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
        public async Task SlashCommandHandler()
        {
            // Defer the response to prevent the "No response" error
            await DeferAsync();

            LogService.SessionDivider('=', "START");
            LogService.Info("[AdventureGameModule.SlashCommandHandler]          > Slash Command /start is executed");

            // Send a follow-up response after the processing is complete.
            await FollowupAsync("Slash Command /start is executed");
            //await FollowupAsync("Your adventure has begun!");
        }
    }
}
