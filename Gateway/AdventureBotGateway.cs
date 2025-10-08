using Adventure.Data;
using Adventure.Loaders;
using Adventure.Services;
using Adventure.TokenAccess;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Adventure.Gateway
{
    /// <summary>
    /// Main entry point for the Adventure Discord bot.
    /// Handles bot initialization, module registration, event handling, and shutdown.
    /// </summary>
    public class AdventureBotGateway
    {
        #region === Fields ===

        private readonly DiscordSocketClient _client;                       // The Discord WebSocket client responsible for connecting and communicating with Discord
        private readonly InteractionService _interactions;                  // Handles slash commands, buttons, modals, etc.
        private readonly IServiceProvider _provider;                        // Provides dependency-injected services to modules
        private readonly CancellationTokenSource _cancellationTokenSource;  // Token used to stop async operations safely on shutdown

        #endregion 

        #region === Constructor ===

        /// <summary>
        /// Initializes a new instance of the <see cref="AdventureBotGateway"/> class.
        /// Subscribes to the ProcessExit event for clean shutdown when the application closes.
        /// </summary>
        public AdventureBotGateway(DiscordSocketClient client, InteractionService interactions, IServiceProvider provider)
        {
            _client = client;
            _interactions = interactions;
            _provider = provider;

            _cancellationTokenSource = new CancellationTokenSource();

            // When the application process exits, ensure the bot disconnects cleanly
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;
        }

        #endregion Constructor

        #region === Bot Startup ===

        /// <summary>
        /// Starts the Adventure bot, loads all game data, connects to Discord, and registers slash command modules.
        /// </summary>
        public async Task StartBotAsync()
        {
            // Load static game data into memory at startup
            GameData.Weapons = WeaponLoader.Load();
            GameData.Armor = ArmorLoader.Load();
            GameData.Items = ItemLoader.Load();
            GameData.Humanoids = HumanoidLoader.Load();
            GameData.Bestiary = BestiaryLoader.Load();

            // Load battle and roll text data
            var (battleText, rollText) = BattleTextLoader.Load();
            GameData.BattleText = battleText;
            GameData.RollText = rollText;

            // Retrieve the bot token securely from CSV
            string discordToken = GetToken.GetTokenFromCSV();

            // Login and connect to Discord using the provided token
            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();

            // Register slash command modules from this assembly
            await _interactions.AddModulesAsync(typeof(AdventureBotGateway).Assembly, _provider);

            // Subscribe to events
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteractionAsync;

            try
            {
                // Keep the bot running indefinitely until manually cancelled
                await Task.Delay(-1, _cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected when shutdown is requested
                LogService.Info("Disconnecting Gateway...");
            }
        }

        #endregion Bot Startup

        #region === Event Handlers ===

        /// <summary>
        /// Triggered when the Discord client is ready.
        /// Registers all slash commands globally.
        /// </summary>
        private async Task ReadyAsync()
        {
            await _interactions.RegisterCommandsGloballyAsync();
        }

        /// <summary>
        /// Handles all incoming interactions (slash commands, buttons, etc.)
        /// by creating an execution context and passing it to the InteractionService.
        /// </summary>
        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, null);
        }

        #endregion Event Handlers

        #region === Logging ===

        /// <summary>
        /// Logs messages from the Discord.NET client.
        /// Filters out irrelevant messages such as unknown channel warnings.
        /// </summary>
        private Task LogAsync(LogMessage log)
        {
            if (!string.IsNullOrEmpty(log.Message) &&
                !log.Message.Contains("unknown channel", StringComparison.OrdinalIgnoreCase))
            {
                LogService.Info(log.ToString());
            }

            // Update bot status log when ready
            if (log.Source == "Gateway" && log.Message == "Ready")
                LogService.BotStatus("ONLINE");

            return Task.CompletedTask;
        }

        #endregion Logging

        #region === Shutdown ===

        /// <summary>
        /// Triggered when the application process exits.
        /// Ensures that the bot disconnects cleanly before termination.
        /// </summary>
        private void OnProcessExit(object sender, EventArgs e)
        {
            // Block until the bot has shut down completely
            ShutdownBotAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Performs a graceful shutdown:
        /// - Cancels active tasks
        /// - Sets bot status to invisible
        /// - Logs out and closes the WebSocket connection
        /// </summary>
        public async Task ShutdownBotAsync()
        {
            try
            {
                LogService.Info("Shutting down bot...");

                _cancellationTokenSource.Cancel();                  // Cancel any long-running tasks

                await _client.SetStatusAsync(UserStatus.Invisible); // Make bot appear offline
                await _client.LogoutAsync();                        // Log out from Discord
                await _client.StopAsync();                          // Close the WebSocket connection

                _cancellationTokenSource.Dispose();                 // Dispose of resources

                LogService.SessionDivider('=', "END");
                LogService.BotStatus("OFFLINE");
            }
            catch (Exception ex)
            {
                // Catch and log any shutdown exceptions
                LogService.Error($"Error while shutting down the bot: {ex.Message}\n{ex}");
            }
        }

        #endregion Shutdown

        #region === Permission Messaging ===

        /// <summary>
        /// Checks if the bot has permission to send messages in a specific channel.
        /// </summary>
        /// <param name="channelId">The channel's unique Discord ID.</param>
        /// <returns>True if the bot can send messages; otherwise false.</returns>
        public async Task<bool> CanSendMessage(ulong channelId)
        {
            var guildChannel = _client.GetChannel(channelId) as IGuildChannel;

            if (guildChannel != null)
            {
                // Get the bot's guild user to check permissions
                var botUser = await guildChannel.Guild.GetCurrentUserAsync();
                var perms = botUser.GetPermissions(guildChannel);

                // Ensure the bot can view and send messages
                return perms.ViewChannel && perms.SendMessages;
            }

            return false;
        }

        #endregion Permission Messaging
    }
}
