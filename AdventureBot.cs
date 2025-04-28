using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Adventure
{
    public class AdventureBot
    {
        private readonly DiscordSocketClient _client; // Client that connects to Discord
        private readonly CommandService _commands; // Command service for handling commands
        private readonly InteractionService _interactions; // Interaction service for slash commands
        private readonly IServiceProvider _services; // The service provider for dependency injection
        private readonly CancellationTokenSource _cancellationTokenSource; // Used to cancel async tasks when closing

        public AdventureBot(
                            DiscordSocketClient client,
                            IServiceProvider services,
                            CommandService commands,
                            InteractionService interactions,
                            CancellationTokenSource cancellationTokenSource
                            )
        {
            // Injected client and other services
            _client = client;
            _commands = commands;
            _interactions = interactions;
            _services = services;
            _cancellationTokenSource = cancellationTokenSource;

            // Register event handler to cleanly shut down the bot when the program exits
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;
        }


        public async Task StartAsync()
        {
            // Subscribe to events for logging, bot readiness, and handling interactions
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.SlashCommandExecuted += OnSlashCommandAsync;
            _client.InteractionCreated += HandleInteractionAsync;

            // Get token and log in to Discord
            string token = GetToken();
            await _client.LoginAsync(TokenType.Bot, token);
            
            Console.WriteLine("[Startup AdventureBot]");
            Debug.WriteLine("[Startup AdventureBot]");

            await _client.StartAsync();

            // Use a CancellationToken to keep the bot running until the program is closed
            await Task.Delay(-1, _cancellationTokenSource.Token);
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"[Bot connected with a latency of {_client.Latency}ms]");
            Debug.WriteLine($"[Bot connected with a latency of {_client.Latency}ms]");

            // Add modules (commands) and register them globally
            await _interactions.AddModulesAsync(typeof(AdventureBot).Assembly, _services);
            await _interactions.RegisterCommandsGloballyAsync(); // Can also register to a specific guild for testing
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            Debug.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, null);
        }


        /// <summary>
        /// Haalt het Discord bot-token op uit het 'get_token.csv' bestand in de projectroot.
        /// </summary>
        private string GetToken()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "get_token.csv");

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath).Trim();
            }
            else
            {
                throw new FileNotFoundException("Token bestand niet gevonden.", filePath);
            }
        }

        /// <summary>
        /// Handles the shutdown process when the program exits.
        /// </summary>
        private async void OnProcessExit(object sender, EventArgs e)
        {
            await ShutdownBotAsync(); // Ensure the bot is cleanly shut down
        }

        /// <summary>
        /// Shuts down the bot gracefully by setting its status to invisible and logging out.
        /// </summary>
        public async Task ShutdownBotAsync()
        {
            try
            {
                // Set the bot status to invisible (offline) before logging out
                await _client.SetStatusAsync(UserStatus.Invisible);
                await _client.LogoutAsync(); // Log the bot out of Discord
                await _client.StopAsync(); // Stop the client from running
            }
            catch (Exception ex)
            {
                // Log any exceptions that may occur during shutdown
                Console.WriteLine("Error while shutting down the bot: " + ex.Message);
                Debug.WriteLine("Error while shutting down the bot: " + ex.Message);
            }
        }


        private async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            //var module = new AdventureGameModule(this); // Zorg dat de juiste module wordt aangeroepen
            await AdventureGameModule.OnSlashCommand(command);
        }

        /// <summary>
        /// Controleert of de bot berichten kan versturen in een specifiek kanaal.
        /// </summary>
        public async Task<bool> CanSendMessage(ulong channelId)
        {
            var guildChannel = _client.GetChannel(channelId) as IGuildChannel;

            if (guildChannel != null)
            {
                var botUser = await guildChannel.Guild.GetCurrentUserAsync();
                var perms = botUser.GetPermissions(guildChannel);

                return perms.ViewChannel && perms.SendMessages;
            }

            return false;
        }
    }
}
