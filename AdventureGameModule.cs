using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Adventure
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AdventureBot _bot;

        public AdventureGameModule(AdventureBot bot)
        {
            _bot = bot;
        }

        // Store player progress using a thread-safe dictionary
        private static readonly ConcurrentDictionary<ulong, GameState> playerStates = new();

        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task StartAdventure()
        {
            // Defer the response to prevent the "No response" error
            await DeferAsync();

            Console.WriteLine("Slash Command /start is being executed");
            Debug.WriteLine("Slash Command /start is being executed");

            // Send a follow-up response after the processing is complete.
            await FollowupAsync("Your adventure has begun!");
        }


        public static async Task OnSlashCommand(SocketSlashCommand command)
        {
            if (command.CommandName == "start")
            {
                await command.RespondAsync("Slash Command /start is being executed");
                Console.WriteLine("Slash Command /start is being executed");
            }
        }
    }
}
