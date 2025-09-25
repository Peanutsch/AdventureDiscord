using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Helpers
{
    public static class LevelHelpers 
    {
        // D&D 5e cumulative XP thresholds for each level
        private static readonly int[] LevelXPThresholds = new int[]
        {
        0,      // Level 1
        300,    // Level 2
        900,    // Level 3
        2700,   // Level 4
        6500,   // Level 5
        14000,  // Level 6
        23000,  // Level 7
        34000,  // Level 8
        48000,  // Level 9
        64000,  // Level 10
        85000,  // Level 11
        100000, // Level 12
        120000, // Level 13
        140000, // Level 14
        165000, // Level 15
        195000, // Level 16
        225000, // Level 17
        265000, // Level 18
        305000, // Level 19
        355000  // Level 20
        };

        /// <summary>
        /// Calculates the player level based on cumulative XP.
        /// </summary>
        /// <param name="xp">The cumulative XP of the player.</param>
        /// <returns>The player level (1–20).</returns>
        public static int GetLevelFromXP(int xp) {
            for (int level = LevelXPThresholds.Length; level > 0; level--) {
                if (xp >= LevelXPThresholds[level - 1])
                    return level;
            }
            return 1; // Minimum level
        }
    }
}
