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
        /// 1. Validate and get Discord user
        /// 2. Initialize player profile
        /// 3. Determine current tile (savepoint or START)
        /// 4. Handle lock switches
        /// 5. Send map to DM
        /// 6. Send channel notification
        /// </summary>
        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("adventure", "Start your adventure in private message.")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();
            LogService.DividerParts(1, "SlashCommand: /adventure");

            // Store guild channel ID for battle notifications
            BattlePrivateMessageHelper.SetGuildChannelId(Context.User.Id, Context.Channel.Id);

            // Step 1: Validate user
            Discord.IUser? user = await ValidateAndGetUserAsync();
            if (user == null) return;

            // Step 2: Initialize player
            PlayerModel? player = await InitializePlayerAsync(user);
            if (player == null) return;

            // Step 3: Determine tile
            TileModel? tile = await DetermineTileAsync(player);
            if (tile == null) return;

            // Step 4: Handle lock switches
            await HandleTileLockSwitchAsync(tile);

            // Step 5: Send map to DM
            bool mapSent = await SendMapToDMAsync(player, tile);
            if (!mapSent) return;

            // Step 6: Notify in channel
            await SendChannelNotificationAsync(player);

            LogService.DividerParts(2, "SlashCommand: /adventure");
        }

        #endregion

        #region === Helper Methods ===

        /// <summary>
        /// Validates and retrieves the Discord user who triggered the command.
        /// </summary>
        /// <returns>The Discord user, or null if validation fails.</returns>
        private async Task<Discord.IUser?> ValidateAndGetUserAsync()
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
        private async Task<PlayerModel?> InitializePlayerAsync(Discord.IUser user)
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
        private async Task HandleTileLockSwitchAsync(TileModel tile)
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

        #endregion
    }
}
