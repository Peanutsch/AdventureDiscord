using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Concurrent;

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
        [SlashCommand("start", "test test test")]
        public async Task StartAdventure()
        {
            // Send a test message confirming the slash command registration
            await RespondAsync("Slash Command registered");
        }
    }
}
