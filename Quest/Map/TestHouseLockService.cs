using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map
{
    public class TestHouseLockService
    {
        /// <summary>
        /// Toggles the locked state of a door or lock linked to a specific tile.
        /// The method checks if the current tile contains a switch (LockSwitch)
        /// and a valid LockId. If both exist, the corresponding lock's state
        /// is inverted (locked ↔ unlocked), and a notification is sent to the guild channel.
        /// </summary>
        /// <param name="currentTile">The current tile where the player stands.</param>
        /// <param name="locks">A dictionary containing all lock IDs and their states.</param>
        /// <param name="userId">Discord user ID of the player toggling the lock.</param>
        /// <param name="playerName">Name of the player toggling the lock.</param>
        /// <returns>
        /// True if a valid lock was toggled successfully; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="currentTile"/> is null.</exception>
        public static async Task<bool> ToggleLockBySwitchAsync(TileModel currentTile, Dictionary<string, TestHouseLockModel> locks, ulong userId, string playerName)
        {
            if (currentTile == null)
                throw new ArgumentNullException(nameof(currentTile));

            // Only proceed if the tile has a switch and a valid lock ID
            if (currentTile.LockSwitch && !string.IsNullOrEmpty(currentTile.LockId))
            {
                // Try to find the matching lock in the dictionary
                if (locks.TryGetValue(currentTile.LockId, out var lockState))
                {
                    // Toggle the lock state (locked <-> unlocked)
                    lockState.Locked = !lockState.Locked;
                    bool isNowLocked = lockState.Locked;

                    // Wrap updated locks dictionary into a TestHouseLockCollection
                    var updatedLocks = new TestHouseLockCollection
                    {
                        LockedDoors = locks
                    };

                    // Save updated lock data to JSON file
                    JsonDataManager.UpdateLockStates(updatedLocks, "testhouselocks.json");

                    LogService.Info($"[TestHouseLockService.ToggleLockBySwitch] Lock '{currentTile.LockId}' is now {(isNowLocked ? "locked" : "unlocked")} by {playerName}.");

                    // Send guild notification
                    await SendLockToggleNotificationAsync(userId, playerName, currentTile.LockId, isNowLocked);

                    // Reload updated lock data
                    ReloadLockStates();
                    return true;
                }
                else
                {
                    LogService.Info($"[TestHouseLockService.ToggleLockBySwitch] No lock found with ID '{currentTile.LockId}'.");
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a notification to the guild channel when a player toggles a lock.
        /// </summary>
        private static async Task SendLockToggleNotificationAsync(ulong userId, string playerName, string lockId, bool isNowLocked)
        {
            try
            {
                ulong channelId = BattlePrivateMessageHelper.GetGuildChannelId(userId);
                if (channelId == 0)
                {
                    LogService.Info($"[TestHouseLockService.SendLockToggleNotificationAsync] No guild channel configured for user {userId}.");
                    return;
                }

                string statusEmoji = isNowLocked ? "🔒" : "🔓";
                string statusText = isNowLocked ? "locked" : "unlocked";

                var embed = new EmbedBuilder()
                    .WithColor(isNowLocked ? Color.Orange : Color.Green)
                    .WithTitle($"{statusEmoji} Lock Status Changed")
                    .WithDescription($"**{playerName}** has `{statusText.ToUpper()}` the lock: `{lockId}`")
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .Build();

                await BattlePrivateMessageHelper.SendGuildMessageUpdateAsync(channelId, embed);
                LogService.Info($"[TestHouseLockService.SendLockToggleNotificationAsync] Sent lock toggle notification to channel {channelId}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[TestHouseLockService.SendLockToggleNotificationAsync] Failed to send notification: {ex.Message}");
            }
        }

        public static void ReloadLockStates()
        {
            // === Load door lock configuration testhouselocks.json ===
            var lockData = JsonDataManager.LoadObjectFromJson<TestHouseLockCollection>("Data/Map/TestHouse/testhouselocks.json");
            if (lockData == null)
            {
                LogService.Error("[TestHouseLockService.ReloadLockStates] Error loading testhouselocks.json: Data is invalid or missing.");
                throw new InvalidDataException("testhouselocks.json is invalid or missing.");
            }
            LogService.Info($"[TestHouseLockService.ReloadLockStates] Reloading testhouselocks.json successfully with {lockData.LockedDoors.Count} entries.");

            // === Apply new lock states to all loaded tiles ===
            foreach (var tile in TestHouseLoader.TileLookup.Values)
            {
                if (!string.IsNullOrEmpty(tile.LockId) && lockData.LockedDoors.TryGetValue(tile.LockId, out var newLock))
                {
                    tile.LockState!.LockType = newLock.LockType;
                    tile.LockState.Locked = newLock.Locked;
                    tile.LockState.KeyId = newLock.KeyId;
                }
            }

            foreach (var kvp in TestHouseLoader.LockLookup.Values)
            {
                LogService.Info($"\n[LockType: {kvp.LockType}] [KeyId: {kvp.KeyId}] [Locked: {kvp.Locked}]\n");
            }

            // === Update LockLookup as well ===
            TestHouseLoader.LockLookup = lockData.LockedDoors.ToDictionary(
                kv => kv.Key,
                kv => kv.Value,
                StringComparer.OrdinalIgnoreCase
            );

            LogService.Info("[TestHouseLockService.ReloadLockStates] Updated TileLookup and LockLookup...");
        }
    }
}
