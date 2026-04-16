using Discord;
using Discord.WebSocket;
using Adventure.Data;
using Adventure.Services;
using Discord.Rest;
using System.Collections.Concurrent;

namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Helper class for managing private message communications during battles.
    /// Centralizes DM sending logic for battle interactions, weapon selection, and battle logs.
    /// This enables battles to be played in private messages instead of public channels,
    /// keeping the main chat clean while showing only final results publicly.
    /// </summary>
    public static class BattlePrivateMessageHelper
    {
        #region === Discord Client Reference ===

        /// <summary>
        /// Static reference to the Discord client, used for sending messages to guild channels.
        /// Set once during bot startup via <see cref="SetClient"/>.
        /// </summary>
        private static DiscordSocketClient? _client;

        /// <summary>
        /// Initializes the Discord client reference for guild channel messaging.
        /// </summary>
        public static void SetClient(DiscordSocketClient client) => _client = client;

        /// <summary>
        /// Returns the Discord client reference for use by other services.
        /// </summary>
        public static DiscordSocketClient? GetClient() => _client;

        #endregion

        #region === Guild Channel Tracking ===

        /// <summary>
        /// Tracks the guild channel ID per user, so battle updates can be sent to the public channel.
        /// Key: Discord user ID | Value: Guild channel ID where the adventure was started.
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, ulong> GuildChannelIds = new();

        /// <summary>
        /// Stores the guild channel ID for a user (called when /start is executed from a guild channel).
        /// </summary>
        public static void SetGuildChannelId(ulong userId, ulong channelId)
        {
            GuildChannelIds[userId] = channelId;
            LogService.Info($"[BattlePrivateMessageHelper.SetGuildChannelId] Stored channel {channelId} for user {userId}");
        }

        /// <summary>
        /// Retrieves the guild channel ID for a user.
        /// Priority: 1) Per-user override from /start, 2) Default from botconfig.json.
        /// </summary>
        /// <returns>The guild channel ID if found, 0 otherwise.</returns>
        public static ulong GetGuildChannelId(ulong userId)
        {
            // 1) Check per-user override (set via /start)
            if (GuildChannelIds.TryGetValue(userId, out ulong channelId))
                return channelId;

            // 2) Fallback to botconfig.json default
            ulong configChannelId = GameData.BotConfig?.GuildChannelId ?? 0;
            if (configChannelId != 0)
            {
                LogService.Info($"[BattlePrivateMessageHelper.GetGuildChannelId] Using default from botconfig.json: {configChannelId}");
                return configChannelId;
            }

            LogService.Info($"[BattlePrivateMessageHelper.GetGuildChannelId] No guild channel configured for user {userId}.");
            return 0;
        }

        /// <summary>
        /// Returns all unique guild channel IDs from both per-user tracking and botconfig.json fallback.
        /// Used for broadcasting status messages (startup/shutdown) to all known guild channels.
        /// </summary>
        public static HashSet<ulong> GetAllUniqueGuildChannelIds()
        {
            var channelIds = new HashSet<ulong>(GuildChannelIds.Values);

            ulong configChannelId = GameData.BotConfig?.GuildChannelId ?? 0;
            if (configChannelId != 0)
                channelIds.Add(configChannelId);

            return channelIds;
        }

        /// <summary>
        /// Sends a battle update embed to the guild channel so other members can follow the fight.
        /// Uses the gateway cache first, then falls back to the REST API if the channel is not cached.
        /// </summary>
        /// <param name="channelId">The guild channel ID to send the update to.</param>
        /// <param name="embed">The embed containing the battle update.</param>
        public static async Task SendGuildMessageUpdateAsync(ulong channelId, Embed embed)
        {
            if (_client == null)
            {
                LogService.Error("[BattlePrivateMessageHelper.SendGuildMessageUpdateAsync] Discord client not set.");
                return;
            }

            try
            {
                // Try gateway cache first
                IMessageChannel? channel = _client.GetChannel(channelId) as IMessageChannel;

                // Fallback to REST API if channel not in gateway cache
                if (channel == null)
                {
                    LogService.Info($"[BattlePrivateMessageHelper.SendGuildMessageUpdateAsync] Channel {channelId} not in cache, trying REST API...");
                    RestChannel restChannel = await _client.Rest.GetChannelAsync(channelId);
                    channel = restChannel as IMessageChannel;
                }

                if (channel != null)
                {
                    await channel.SendMessageAsync(embed: embed);
                    LogService.Info($"[BattlePrivateMessageHelper.SendGuildMessageUpdateAsync] Battle update sent to channel {channelId}");
                }
                else
                {
                    LogService.Error($"[BattlePrivateMessageHelper.SendGuildMessageUpdateAsync] Channel {channelId} not found via cache or REST.");
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"[BattlePrivateMessageHelper.SendGuildMessageUpdateAsync] Failed to send guild update: {ex.Message}");
            }
        }

        #endregion

        #region === Private Message Management ===
        /// <summary>
        /// Gets the Discord user from an interaction and sends them a DM.
        /// </summary>
        /// <param name="interaction">The Discord interaction containing the user.</param>
        /// <param name="embed">The embed to send in the DM.</param>
        /// <param name="components">Optional components (buttons) to include.</param>
        /// <returns>The sent message, or null if sending failed.</returns>
        public static async Task<IUserMessage?> SendBattleMessageAsync(
            SocketInteraction interaction,
            Embed embed,
            MessageComponent? components = null)
        {
            try
            {
                SocketUser user = interaction.User as SocketUser;
                if (user == null)
                {
                    LogService.Error("[BattlePrivateMessageHelper.SendBattleMessageAsync] Unable to get user from interaction.");
                    return null;
                }

                IDMChannel dmChannel = await user.CreateDMChannelAsync();
                if (dmChannel == null)
                {
                    LogService.Error("[BattlePrivateMessageHelper.SendBattleMessageAsync] Unable to create DM channel.");
                    return null;
                }

                IUserMessage message = await dmChannel.SendMessageAsync(embed: embed, components: components);
                LogService.Info($"[BattlePrivateMessageHelper.SendBattleMessageAsync] Battle message sent to {user.Username}");
                return message;
            }
            catch (Exception ex)
            {
                LogService.Error($"[BattlePrivateMessageHelper.SendBattleMessageAsync] Failed to send DM: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates an existing DM message with new embed and components.
        /// Used during active battle to show updated battle status.
        /// </summary>
        /// <param name="dmMessage">The existing DM message to update.</param>
        /// <param name="embed">The new embed to display.</param>
        /// <param name="components">Optional components (buttons) to include.</param>
        /// <returns>True if update succeeded, false otherwise.</returns>
        public static async Task<bool> UpdateBattleMessageAsync(
            IUserMessage dmMessage,
            Embed embed,
            MessageComponent? components = null)
        {
            try
            {
                await dmMessage.ModifyAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components;
                });

                LogService.Info("[BattlePrivateMessageHelper.UpdateBattleMessageAsync] Battle message updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Error($"[BattlePrivateMessageHelper.UpdateBattleMessageAsync] Failed to update DM: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stores a reference to the active battle DM message for a user.
        /// Allows us to update the message as the battle progresses.
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, ulong> ActiveBattleMessages = new();

        /// <summary>
        /// Records the DM message ID for an active battle.
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        /// <param name="messageId">The message ID of the DM.</param>
        public static void SetActiveBattleMessage(ulong userId, ulong messageId)
        {
            ActiveBattleMessages[userId] = messageId;
            LogService.Info($"[BattlePrivateMessageHelper.SetActiveBattleMessage] Stored message {messageId} for user {userId}");
        }

        /// <summary>
        /// Retrieves the active battle DM message ID for a user.
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        /// <returns>The message ID if found, 0 otherwise.</returns>
        public static ulong GetActiveBattleMessage(ulong userId)
        {
            if (ActiveBattleMessages.TryGetValue(userId, out ulong messageId))
            {
                LogService.Info($"[BattlePrivateMessageHelper.GetActiveBattleMessage] Retrieved message {messageId} for user {userId}");
                return messageId;
            }

            LogService.Error($"[BattlePrivateMessageHelper.GetActiveBattleMessage] No message found for user {userId}. Active messages: {string.Join(", ", ActiveBattleMessages.Keys)}");
            return 0;
        }

        /// <summary>
        /// NOT IN USE. 
        /// (Battle log messages are sent as new messages, no need to track the active battle message ID.)
        /// Clears the active battle message reference for a user (e.g., when battle ends).
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        public static void ClearActiveBattleMessage(ulong userId)
        {
            ActiveBattleMessages.TryRemove(userId, out _);
        }
        #endregion

        #region === Shutdown: Disable Active Buttons ===
        /// <summary>
        /// Disables all buttons on active DM messages for all tracked players.
        /// Called during bot shutdown to prevent stale button interactions.
        /// Falls back to searching recent DM messages if no tracked message ID exists.
        /// </summary>
        public static async Task DisableAllActiveButtonsAsync()
        {
            if (_client == null)
            {
                LogService.Error("[BattlePrivateMessageHelper.DisableAllActiveButtonsAsync] Discord client not set.");
                return;
            }

            List<ulong> playerIds = ActivePlayerTracker.GetAllActivePlayerIds();
            LogService.Info($"[BattlePrivateMessageHelper.DisableAllActiveButtonsAsync] Disabling buttons for {playerIds.Count} active player(s)...");

            foreach (var userId in playerIds)
            {
                await DisableButtonsForUserAsync(userId);
            }
        }

        /// <summary>
        /// Disables buttons for a single user's active battle message.
        /// Handles errors gracefully and logs results.
        /// </summary>
        private static async Task DisableButtonsForUserAsync(ulong userId)
        {
            try
            {
                SocketUser? user = _client?.GetUser(userId);
                if (user == null)
                {
                    LogService.Error($"[BattlePrivateMessageHelper.DisableButtonsForUserAsync] User {userId} not found in cache, skipping.");
                    return;
                }

                IDMChannel dmChannel = await user.CreateDMChannelAsync();
                IUserMessage? targetMessage = await FindTargetMessageAsync(userId, dmChannel);

                if (targetMessage == null)
                {
                    LogService.Info($"[BattlePrivateMessageHelper.DisableButtonsForUserAsync] No message with active buttons found for user {userId}.");
                    return;
                }

                ComponentBuilder builder = BuildDisabledButtonsComponent(targetMessage);
                await targetMessage.ModifyAsync(msg => msg.Components = builder.Build());

                LogService.Info($"[BattlePrivateMessageHelper.DisableButtonsForUserAsync] ✅ Disabled buttons for user {user.Username}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[BattlePrivateMessageHelper.DisableButtonsForUserAsync] Failed for user {userId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the target message to disable buttons on.
        /// First tries the tracked message ID, then searches recent DM messages.
        /// </summary>
        private static async Task<IUserMessage?> FindTargetMessageAsync(ulong userId, IDMChannel dmChannel)
        {
            // Try tracked message ID first
            ulong messageId = GetActiveBattleMessage(userId);
            if (messageId != 0)
            {
                IMessage? msg = await dmChannel.GetMessageAsync(messageId);
                IUserMessage? userMsg = msg as IUserMessage;

                if (userMsg != null && MessageHasActiveButtons(userMsg))
                    return userMsg;
            }

            // Fallback: search recent DM messages
            return await FindMessageWithActiveButtonsAsync(userId, dmChannel);
        }

        /// <summary>
        /// Searches recent DM messages for a bot message with active buttons.
        /// </summary>
        private static async Task<IUserMessage?> FindMessageWithActiveButtonsAsync(ulong userId, IDMChannel dmChannel)
        {
            LogService.Info($"[BattlePrivateMessageHelper.FindMessageWithActiveButtonsAsync] Searching recent DMs for user {userId}...");

            var recentMessages = await dmChannel.GetMessagesAsync(limit: 20).FlattenAsync();

            foreach (var recentMsg in recentMessages)
            {
                if (recentMsg is not IUserMessage userMsg) continue;
                if (recentMsg.Author.Id != _client?.CurrentUser.Id) continue;
                if (!MessageHasActiveButtons(userMsg)) continue;

                LogService.Info($"[BattlePrivateMessageHelper.FindMessageWithActiveButtonsAsync] Found active message {userMsg.Id} for user {userId}");
                return userMsg;
            }

            return null;
        }

        /// <summary>
        /// Checks if a message has any active (enabled) buttons.
        /// </summary>
        private static bool MessageHasActiveButtons(IUserMessage message)
        {
            if (message.Components.Count == 0)
                return false;

            foreach (var actionRow in message.Components)
            {
                if (actionRow is not ActionRowComponent rowComponent) continue;

                foreach (var component in rowComponent.Components)
                {
                    if (component is ButtonComponent button && !button.IsDisabled)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds a ComponentBuilder with all buttons from the message disabled.
        /// </summary>
        private static ComponentBuilder BuildDisabledButtonsComponent(IUserMessage message)
        {
            var builder = new ComponentBuilder();
            int row = 0;

            foreach (var actionRow in message.Components)
            {
                if (actionRow is not ActionRowComponent rowComponent)
                    continue;

                foreach (var component in rowComponent.Components)
                {
                    if (component is ButtonComponent button)
                    {
                        builder.WithButton(
                            button.Label ?? "...",
                            button.CustomId ?? $"disabled_{row}",
                            button.Style,
                            disabled: true,
                            row: row);
                    }
                }
                row++;
            }

            return builder;
        }
        #endregion
    }
}