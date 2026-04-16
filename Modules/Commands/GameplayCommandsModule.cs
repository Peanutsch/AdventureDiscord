using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Models.Player;
using Adventure.Modules.Helpers;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Adventure.Modules.Commands
{
    /// <summary>
    /// Discord slash command module for core gameplay functionality.
    /// 
    /// This module provides commands for:
    /// - Starting and initializing player adventures
    /// - Managing player progression and world exploration
    /// - Sending map embeds and navigation to private messages
    /// 
    /// All gameplay interactions are routed to Discord DMs for privacy and cleaner channel experience.
    /// </summary>
    public class GameplayCommandsModule : InteractionModuleBase<SocketInteractionContext>
    {
        #region === Main Command Handler ===

        /// <summary>
        /// Handles the /adventure slash command. Initializes the player's adventure session,
        /// loads their current position, and sends the map embed with navigation to their DM.
        /// 
        /// Orchestrates the full adventure startup flow:
        /// 1. Validate no active session (with inactivity cleanup)
        /// 2. Validate and get Discord user
        /// 3. Initialize player profile
        /// 4. Determine current tile (savepoint or START)
        /// 5. Handle lock switches
        /// 6. Notify if session was recently reset (cleanup notification)
        /// 7. Send map to DM
        /// 8. Send channel notification
        /// 9. Set player state to InAdventure
        /// </summary>
        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("adventure", "Start your adventure in private message.")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();
            LogService.DividerParts(1, "SlashCommand: /adventure");

            // Store guild channel ID for battle notifications
            BattlePrivateMessageHelper.SetGuildChannelId(Context.User.Id, Context.Channel.Id);

            // Step 1: Validate no active session
            bool sessionValid = await ValidateNoActiveSessionAsync();
            if (!sessionValid) return;

            // Step 2: Validate user
            Discord.IUser? user = await ValidateAndGetUserAsync();
            if (user == null) return;

            // Step 3: Initialize player
            PlayerModel? player = await InitializePlayerAsync(user);
            if (player == null) return;

            // Step 4: Determine tile
            TileModel? tile = await DetermineTileAsync(player);
            if (tile == null) return;

            // Step 5: Handle lock switches
            await HandleTileLockSwitchAsync(tile);

            // Step 6: Check if session was recently reset and notify player FIRST
            await NotifyIfSessionWasResetAsync(player);

            // Step 7: Send map to DM
            bool mapSent = await SendMapToDMAsync(player, tile);
            if (!mapSent) return;

            // Step 8: Notify in channel
            await SendChannelNotificationAsync(player);

            // Step 9: Set player state to InAdventure and update activity time
            SetPlayerStateInAdventure(player);

            LogService.DividerParts(2, "SlashCommand: /adventure");
        }

        #endregion

        #region === Player stats command ===
        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("stats", "View your player stats.")]
        public async Task SlashCommandStatsHandler()
        {
            await DeferAsync();
            LogService.DividerParts(1, "SlashCommand: /stats");

            // Validate user
            Discord.IUser? user = await ValidateAndGetUserAsync();
            if (user == null) return;

            // Get player
            PlayerModel? player = await GetPlayerForStatsAsync(user);
            if (player == null) return;

            // Build and send stats embed
            await SendStatsEmbedAsync(player);

            LogService.DividerParts(2, "SlashCommand: /stats");
        }
        #endregion

        #region === Helper Methods ===

        /// <summary>
        /// Validates that the player does not have an active adventure or battle session.
        /// If a session is stuck (> 5 minutes old), auto-cleanup and allow new session.
        /// </summary>
        /// <returns>True if player can start a new session; false if actively in one.</returns>
        private async Task<bool> ValidateNoActiveSessionAsync()
        {
            PlayerModel? player = SlashCommandHelpers.GetOrCreatePlayer(Context.User.Id, "");
            if (player == null)
            {
                await FollowupAsync("⚠️ Error loading player data.");
                return false;
            }

            if (player.CurrentState != PlayerState.Idle)
            {
                TimeSpan inactivityTime = DateTime.UtcNow - player.LastActivityTime;
                const int INACTIVITY_TIMEOUT_MINUTES = 5;

                // Auto-cleanup if session is stuck (> 5 minutes old)
                if (inactivityTime > TimeSpan.FromMinutes(INACTIVITY_TIMEOUT_MINUTES))
                {
                    LogService.Info($"[/adventure] Player {Context.User.Id} session stuck (inactive {inactivityTime.TotalMinutes:F1}min). Auto-cleanup.");
                    player.CurrentState = PlayerState.Idle;
                    JsonDataManager.UpdatePlayerState(Context.User.Id, PlayerState.Idle);
                    return true;  // Allow new session
                }

                // Session is recent, block it
                string sessionType = player.CurrentState == PlayerState.InAdventure ? "adventure" : "battle";
                await FollowupAsync($"⚠️ You already have an active {sessionType} session.");
                LogService.Info($"[/adventure] Player {Context.User.Id} attempted to start adventure while in {player.CurrentState} state.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates and retrieves the Discord user who triggered the command.
        /// </summary>
        /// <returns>The Discord user, or null if validation fails.</returns>
        private async Task<IUser?> ValidateAndGetUserAsync()
        {
            Discord.IUser? user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return null;
            }

            LogService.Info($"[/adventure] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");
            return user;
        }

        /// <summary>
        /// Initializes or retrieves the player profile.
        /// </summary>
        /// <param name="user">The Discord user.</param>
        /// <returns>The player model, or null if initialization fails.</returns>
        private async Task<PlayerModel?> InitializePlayerAsync(IUser user)
        {
            PlayerModel player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return null;
            }

            return player;
        }

        /// <summary>
        /// Gets or creates a player for the stats command.
        /// </summary>
        /// <param name="user">The Discord user.</param>
        /// <returns>The player model, or null if retrieval fails.</returns>
        private async Task<PlayerModel?> GetPlayerForStatsAsync(IUser user)
        {
            PlayerModel? player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Error loading player data.");
                return null;
            }

            LogService.Info($"[/stats] Retrieved player stats for {user.GlobalName ?? user.Username}");
            return player;
        }

        /// <summary>
        /// Builds a stats embed for the given player.
        /// </summary>
        /// <param name="player">The player to display stats for.</param>
        /// <returns>An embed containing the player's stats.</returns>
        private static Embed BuildStatsEmbed(PlayerModel player)
        {
            return new EmbedBuilder()
                .WithTitle($"{player.Name}'s Stats")
                .WithColor(Color.Green)
                .AddField("Level", player.Level, true)
                .AddField("HP", $"{player.Hitpoints}", true)
                .AddField("Experience", $"{player.XP}", true)
                .AddField("Strength", player.Attributes.Strength, true)
                .AddField("Dexterity", player.Attributes.Dexterity, true)
                .AddField("Constitution", player.Attributes.Constitution, true)
                .AddField("Intelligence", player.Attributes.Intelligence, true)
                .AddField("Wisdom", player.Attributes.Wisdom, true)
                .AddField("Charisma", player.Attributes.Charisma, true)
                //.AddField("Gold", player.Gold, true)
                .WithFooter("Keep adventuring to improve your stats!")
                .Build();
        }

        /// <summary>
        /// Sends the stats embed to the user as an ephemeral message.
        /// </summary>
        /// <param name="player">The player whose stats to display.</param>
        private async Task SendStatsEmbedAsync(PlayerModel player)
        {
            Embed statsEmbed = BuildStatsEmbed(player);
            await FollowupAsync(embed: statsEmbed, ephemeral: true);
            LogService.Info($"[/stats] Stats embed sent for player {player.Name}");
        }

        /// <summary>
        /// Determines the current tile for the player.
        /// Uses savepoint if valid, otherwise falls back to START tile.
        /// </summary>
        /// <param name="player">The player profile.</param>
        /// <returns>The tile model, or null if no valid tile found.</returns>
        private async Task<TileModel?> DetermineTileAsync(PlayerModel player)
        {
            // Try to get tile from savepoint
            TileModel? tile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint);

            // Fallback to START tile if savepoint invalid
            if (tile == null)
            {
                LogService.Info($"[/adventure] Savepoint '{player.Savepoint}' invalid. Fallback to START tile.");
                tile = SlashCommandHelpers.FindStartTile();

                if (tile == null)
                {
                    await FollowupAsync("❌ No START tile found in any area. Cannot start.", ephemeral: true);
                    return null;
                }

                // Update player's savepoint to START tile
                player.Savepoint = $"{tile.AreaId}:{tile.TilePosition}";
                JsonDataManager.UpdatePlayerSavepoint(Context.User.Id, player.Savepoint);
                LogService.Info($"[/adventure] Position saved as new savepoint: {player.Savepoint}");
            }

            return tile;
        }

        /// <summary>
        /// Handles lock switches for the current tile.
        /// This allows tiles to act as triggers for door locks.
        /// </summary>
        /// <param name="tile">The current tile.</param>
        private static async Task HandleTileLockSwitchAsync(TileModel tile)
        {
            if (TestHouseLockService.ToggleLockBySwitch(tile, TestHouseLoader.LockLookup))
            {
                LogService.Info($"[/adventure] Tile {tile.TileId} switch toggled lock: {tile.LockId}");
            }

            // Log area information
            string areaName = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out TestHouseAreaModel? area)
                ? area.Name
                : "Unknown Area";
            LogService.Info($"[/adventure] Starting in area: {areaName}, position: {tile.TilePosition}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Builds and sends the map embed with navigation buttons to the player's DM.
        /// </summary>
        /// <param name="player">The player profile.</param>
        /// <param name="tile">The current tile.</param>
        /// <returns>True if message was sent successfully; false otherwise.</returns>
        private async Task<bool> SendMapToDMAsync(PlayerModel player, TileModel tile)
        {
            // Build embed and buttons
            EmbedBuilder embed = EmbedBuildersMap.EmbedWalk(tile, Context.User.Id);
            ComponentBuilder components = ButtonBuildersMap.BuildDirectionButtons(tile);

            // Track active player position
            await ActivePlayerTracker.UpdatePositionAsync(Context.User.Id, player.Name!, tile.TileId);

            // Send to DM
            IUserMessage? dmMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                Context.Interaction,
                embed.Build(),
                components?.Build());

            if (dmMessage != null)
            {
                LogService.Info("[/adventure] Adventure map sent to DM.");
                return true;
            }

            await FollowupAsync("⚠️ Failed to send adventure map to DM.");
            return false;
        }

        /// <summary>
        /// Sends a notification in the guild channel informing the player to check DMs.
        /// </summary>
        /// <param name="player">The player profile.</param>
        private async Task SendChannelNotificationAsync(PlayerModel player)
        {
            await FollowupAsync($"🗺️ {player.Name}, your adventure has started! Check your DMs to explore.");
        }

        /// <summary>
        /// Checks if the player's session was recently reset due to bot restart/cleanup.
        /// If so, notifies the player in DM about what happened.
        /// </summary>
        /// <param name="player">The player to check.</param>
        private async Task NotifyIfSessionWasResetAsync(PlayerModel player)
        {
            if (player.LastSessionResetTime.HasValue)
            {
                TimeSpan timeSinceReset = DateTime.UtcNow - player.LastSessionResetTime.Value;

                // Only notify if reset happened recently (within last 5 minutes)
                if (timeSinceReset < TimeSpan.FromMinutes(5))
                {
                    try
                    {
                        SocketUser? user = Context.User as SocketUser;
                        if (user != null)
                        {
                            IDMChannel dmChannel = await user.CreateDMChannelAsync();

                            var resetEmbed = new EmbedBuilder()
                                .WithColor(Color.Orange)
                                .WithTitle("⚠️ Session Cleanup Notice")
                                //.WithDescription("Your adventure was interrupted and reset to idle due to a bot restart.")
                                .WithDescription("Your adventure session status is reset to idle due to a bot restart.")
                                .AddField("What happened?", "The bot detected a stuck session and reset it to allow you to play again.")
                                .AddField("What now?", "Your adventure continues as normal. Check the map and keep exploring!")
                                .WithFooter("Session cleanup time: " + player.LastSessionResetTime.Value.ToString("g"))
                                .Build();

                            await dmChannel.SendMessageAsync(embed: resetEmbed);
                            LogService.Info($"[/adventure] Sent session reset notification to player {Context.User.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"[GameplayCommandsModule.NotifyIfSessionWasResetAsync] Failed to send notification: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Sets the player's state to InAdventure and updates activity time in JSON.
        /// </summary>
        /// <param name="player">The player to update.</param>
        private void SetPlayerStateInAdventure(PlayerModel player)
        {
            player.CurrentState = PlayerState.InAdventure;
            player.LastActivityTime = DateTime.UtcNow;
            JsonDataManager.UpdatePlayerState(Context.User.Id, PlayerState.InAdventure);
            JsonDataManager.UpdatePlayerLastActivityTime(Context.User.Id);
            LogService.Info($"[/adventure] Player {Context.User.Id} state set to InAdventure, activity time updated.");
        }

        #endregion
    }
}
