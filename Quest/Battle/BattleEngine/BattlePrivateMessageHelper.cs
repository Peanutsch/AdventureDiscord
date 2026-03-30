using Discord;
using Discord.WebSocket;
using Adventure.Services;

namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Helper class for managing private message communications during battles.
    /// Centralizes DM sending logic for battle interactions, weapon selection, and battle logs.
    /// 
    /// This enables battles to be played in private messages instead of public channels,
    /// keeping the main chat clean while showing only final results publicly.
    /// </summary>
    public static class BattlePrivateMessageHelper
    {
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
                var user = interaction.User as SocketUser;
                if (user == null)
                {
                    LogService.Error("[BattlePrivateMessageHelper] Unable to get user from interaction.");
                    return null;
                }

                var dmChannel = await user.CreateDMChannelAsync();
                if (dmChannel == null)
                {
                    LogService.Error("[BattlePrivateMessageHelper] Unable to create DM channel.");
                    return null;
                }

                var message = await dmChannel.SendMessageAsync(embed: embed, components: components);
                LogService.Info($"[BattlePrivateMessageHelper] Battle message sent to {user.Username}");
                return message;
            }
            catch (Exception ex)
            {
                LogService.Error($"[BattlePrivateMessageHelper] Failed to send DM: {ex.Message}");
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

                LogService.Info("[BattlePrivateMessageHelper] Battle message updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LogService.Error($"[BattlePrivateMessageHelper] Failed to update DM: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stores a reference to the active battle DM message for a user.
        /// Allows us to update the message as the battle progresses.
        /// </summary>
        private static readonly Dictionary<ulong, ulong> ActiveBattleMessages = new();

        /// <summary>
        /// Records the DM message ID for an active battle.
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        /// <param name="messageId">The message ID of the DM.</param>
        public static void SetActiveBattleMessage(ulong userId, ulong messageId)
        {
            lock (ActiveBattleMessages)
            {
                ActiveBattleMessages[userId] = messageId;
            }
        }

        /// <summary>
        /// Retrieves the active battle DM message ID for a user.
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        /// <returns>The message ID if found, 0 otherwise.</returns>
        public static ulong GetActiveBattleMessage(ulong userId)
        {
            lock (ActiveBattleMessages)
            {
                return ActiveBattleMessages.TryGetValue(userId, out var messageId) ? messageId : 0;
            }
        }

        /// <summary>
        /// Clears the active battle message reference for a user (e.g., when battle ends).
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        public static void ClearActiveBattleMessage(ulong userId)
        {
            lock (ActiveBattleMessages)
            {
                ActiveBattleMessages.Remove(userId);
            }
        }
    }
}
