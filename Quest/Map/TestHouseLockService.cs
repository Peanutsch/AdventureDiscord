using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
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
        /// is inverted (locked ↔ unlocked).
        /// </summary>
        /// <param name="currentTile">The current tile where the player stands.</param>
        /// <param name="locks">A dictionary containing all lock IDs and their states.</param>
        /// <returns>
        /// True if a valid lock was toggled successfully; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="currentTile"/> is null.</exception>
        public static bool ToggleLockBySwitch(TileModel currentTile, Dictionary<string, TestHouseLockModel> locks)
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

                    // Wrap updated locks dictionary into a TestHouseLockCollection
                    var updatedLocks = new TestHouseLockCollection
                    {
                        LockedDoors = locks
                    };

                    // Save updated lock data to JSON file
                    JsonDataManager.UpdateLockStates(updatedLocks, "testhouselocks.json");

                    LogService.Info($"[TestHouseLockService.ToggleLockBySwitch] Lock '{currentTile.LockId}' is now {(lockState.Locked ? "locked" : "unlocked")}.");

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
                LogService.Info($"\nLockType: {kvp.LockType}\n" +
                                $"KeyId: {kvp.KeyId}\n" +
                                $"Locked: {kvp.Locked}\n");
            }

            // === Update LockLookup as well ===
            TestHouseLoader.LockLookup = lockData.LockedDoors.ToDictionary(
                kv => kv.Key,
                kv => kv.Value,
                StringComparer.OrdinalIgnoreCase
            );

            LogService.Info("[TestHouseLockService.ReloadLockStates] Updated TileLookup and LockLookup...");
        }

        public string GetLockState(TileModel tile, string pos)
        {
            string result = "open";

            if (tile.LockState!.Locked)
            {
                result = "op slot";
            }

            return result;
        }
    }
}
