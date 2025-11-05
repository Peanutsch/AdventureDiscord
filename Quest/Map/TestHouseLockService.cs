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

                    // Print the updated lock state to the console for debugging
                    LogService.Info($"Lock '{currentTile.LockId}' is now {(lockState.Locked ? "locked" : "unlocked")}.");
                    return true;
                }
                else
                {
                    // No matching lock found in the dictionary
                    LogService.Info($"No lock found with ID '{currentTile.LockId}'.");
                }
            }

            // Return false if no switch was triggered or the lock was not found
            return false;
        }
    }
}
