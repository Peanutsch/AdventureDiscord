using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace Adventure
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Store player progress using a thread-safe dictionary
        private static ConcurrentDictionary<ulong, GameState> playerStates = new();

        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Lets star in the Inn's Diner Room!")]
        public async Task StartAdventure()
        {
            var state = new GameState
            {
                Room = "Inn_Diner_Room"
            };
            playerStates[Context.User.Id] = state;

            await RespondWithRoom(state);
        }

        /// <summary>
        /// Responds with the current room description and available options as buttons.
        /// </summary>
        private async Task RespondWithRoom(GameState state)
        {
            string description = !string.IsNullOrEmpty(state.Room) && RoomDescriptions.ContainsKey(state.Room) ? RoomDescriptions[state.Room] : "Error Room."; // Fallback-tekst


            var builder = new ComponentBuilder();
            foreach (var option in RoomOptions[state.Room!])
            {
                builder.WithButton(option.Description, option.Id);
            }

            await RespondAsync(description, components: builder.Build());
        }

        /// <summary>
        /// Handles button interactions and updates game state based on user choices.
        /// </summary>
        [ComponentInteraction("*")]
        public async Task HandleChoice(string id)
        {
            var userId = Context.User.Id;
            if (!playerStates.TryGetValue(userId, out var state))
            {
                await RespondAsync("Gebruik eerst /start om het spel te beginnen.", ephemeral: true);
                return;
            }

            if (id == "look")
            {
                // Perform 'Look' action: Display the room description again or additional details
                state.Room = "Inn_Diner_Room";
            }
            else if (id == "get")
            {
                // Perform 'Get' action: Add items to inventory, etc.
                state.Room = "Inn_Diner_Room";
            }
            else if (id == "kitchen")
            {
                // Move to the kitchen
                state.Room = "Inn_Kitchen";
            }

            await RespondWithRoom(state);
        }

        /// <summary>
        /// Text descriptions of each room.
        /// </summary>
        private static readonly Dictionary<string, string> RoomDescriptions = new()
        {
            ["Inn_Diner_Room"] = "Ah, the dining room! The most important room of the inn. \nOn the tables are some plates, cutlery, and candles. \nTorches hang on the wall. Because of the low light, you can't see much in the shadows."
        };

        /// <summary>
        /// Options (buttons) for each room.
        /// </summary>
        private static readonly Dictionary<string, List<RoomOption>> RoomOptions = new()
        {
            ["Inn_Diner_Room"] = new() {
            new RoomOption("Look around", "look"),
            new RoomOption("Pick up item", "get"),
            new RoomOption("Go to Kitchen", "kitchen") }
        };
    }
}
