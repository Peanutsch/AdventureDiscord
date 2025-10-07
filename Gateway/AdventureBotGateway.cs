using Adventure.Data;
using Adventure.Loaders;
using Adventure.Services;
using Adventure.TokenAccess;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Adventure.Gateway
{
    public class AdventureBotGateway
    {
        #region === Field ===
        private readonly DiscordSocketClient _client;       // Main client used to connect to Discord
        private readonly InteractionService _interactions;  // Handles interaction modules (e.g., slash commands)
        private readonly IServiceProvider _provider;        // Provides access to registered services via dependency injection

        private readonly CancellationTokenSource _cancellationTokenSource; // Used to cancel async tasks when closing
        #endregion 

        #region === Constructor ===
        public AdventureBotGateway(DiscordSocketClient client, InteractionService interactions, IServiceProvider provider)
        {
            _client = client;
            _interactions = interactions;
            _provider = provider;
            
            _cancellationTokenSource = new CancellationTokenSource();

            // Register event handler to cleanly shut down the bot when the program exits
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;
        }
        #endregion Constructor

        #region === Bot Startup ===
        public async Task StartBotAsync()
        {
            // Load GameData
            GameData.Weapons = WeaponLoader.Load();
            GameData.Armor = ArmorLoader.Load();
            GameData.Items = ItemLoader.Load();
            GameData.Humanoids = HumanoidLoader.Load();
            GameData.Bestiary = BestiaryLoader.Load();

            // Load text data
            var (battleText, rollText) = BattleTextLoader.Load();
            GameData.BattleText = battleText;
            GameData.RollText = rollText;

            string discordToken = GetToken.GetTokenFromCSV();

            await _client.LoginAsync(TokenType.Bot, discordToken);                                // Authenticate the bot
            await _client.StartAsync();                                                           // Start the WebSocket connection to Discord
            await _interactions.AddModulesAsync(typeof(AdventureBotGateway).Assembly, _provider); // Register slash command modules

            // Subscribe to events for logging, bot readiness, and handling interactions
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteractionAsync;

            try
            {
                await Task.Delay(-1, _cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                LogService.Info("Bot offline");
            }
        }
        #endregion Bot Startup

        #region === Event Handlers ===
        private async Task ReadyAsync()
        {
            // Add modules (commands) and register them globally
            await _interactions.RegisterCommandsGloballyAsync(); 
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, null);
        }
        #endregion Event Handlers

        #region === Logging ===
        private Task LogAsync(LogMessage log)
        {
            if (!string.IsNullOrEmpty(log.Message) &&
                !log.Message.Contains("unknown channel", StringComparison.OrdinalIgnoreCase))
            {
                LogService.Info(log.ToString());
            }

            // Bot status logging
            if (log.Source == "Gateway" && log.Message == "Ready")
                LogService.BotStatus("ONLINE");

            return Task.CompletedTask;
        }
        #endregion Logging

        #region === Shutdown ===
        /// <summary>
        /// Handles the shutdown process when the program exits.
        /// </summary>
        private void OnProcessExit(object sender, EventArgs e)
        {
            ShutdownBotAsync().GetAwaiter().GetResult(); // Ensure bot shuts down before process exits
        }

        /// <summary>
        /// Shuts down the bot gracefully by setting its status to invisible and logging out.
        /// </summary>
        public async Task ShutdownBotAsync()
        {
            try
            {
                LogService.Info("Shutting down bot...");

                _cancellationTokenSource.Cancel();                  // Signal cancellation to any running tasks
                await _client.SetStatusAsync(UserStatus.Invisible); // Set status to offline
                await _client.LogoutAsync();                        // Log out of Discord
                await _client.StopAsync();                          // Close the WebSocket connection
                
                _cancellationTokenSource.Dispose(); // Release unmanaged resources

                LogService.SessionDivider('=', "END");
                LogService.BotStatus("OFFLINE");
            }
            catch (Exception ex)
            {
                // Log any exceptions that may occur during shutdown
                LogService.Error($"Error while shutting down the bot: {ex.Message}\n{ex}");
            }
        }
        #endregion Shutdown

        #region === Permission Messaging ===
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
        #endregion Permission Messaging
    }
}
