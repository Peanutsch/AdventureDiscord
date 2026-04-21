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
            var (sessionValid, sessionError) = SlashCommandHelpers.ValidateNoActiveSession(Context.User.Id);
            if (!sessionValid)
            {
                await FollowupAsync(sessionError ?? "⚠️ Unknown error.");
                return;
            }

            // Step 2: Validate user
            IUser? user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.Info($"[/adventure] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            // Step 3: Initialize player
            PlayerModel? player = SlashCommandHelpers.InitializePlayer(user);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            // Step 4: Determine tile
            var (tile, tileError) = SlashCommandHelpers.DetermineTile(player, Context.User.Id);
            if (tile == null)
            {
                await FollowupAsync(tileError ?? "❌ Could not determine player position.");
                return;
            }

            // Step 5: Send map to DM (lock toggle happens in EmbedWalkAsync)
            bool mapSent = await SendMapToDMAsync(player, tile);
            if (!mapSent) return;

            // Step 6: Notify in channel
            await SendChannelNotificationAsync(player);

            // Step 7: Set player state to InAdventure and update activity time
            SlashCommandHelpers.SetPlayerStateInAdventure(player, Context.User.Id);

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
            IUser? user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            // Get player
            PlayerModel? player = SlashCommandHelpers.GetPlayerForStats(user);
            if (player == null)
            {
                await FollowupAsync("⚠️ Error loading player data.");
                return;
            }

            // Build and send stats embed
            Embed statsEmbed = SlashCommandHelpers.BuildStatsEmbed(player);
            await FollowupAsync(embed: statsEmbed, ephemeral: true);
            LogService.Info($"[/stats] Stats embed sent for player {player.Name}");

            LogService.DividerParts(2, "SlashCommand: /stats");
        }
        #endregion

        #region === Player Ability Score Improvements ===

        #endregion

        #region === Helper Methods ===

        /// <summary>
        /// Builds and sends the map embed with navigation buttons to the player's DM.
        /// </summary>
        /// <param name="player">The player profile.</param>
        /// <param name="tile">The current tile.</param>
        /// <returns>True if message was sent successfully; false otherwise.</returns>
        private async Task<bool> SendMapToDMAsync(PlayerModel player, TileModel tile)
        {
            // Build embed and buttons
            EmbedBuilder embed = await EmbedBuildersMap.EmbedWalkAsync(tile, Context.User.Id, player.Name!);
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
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithDescription($"🗺️ Hello **{player.Name}**, your adventure has started! Check your DMs to explore.")
                .Build();

            await FollowupAsync(embed: embed);
        }

        #endregion
    }
}
