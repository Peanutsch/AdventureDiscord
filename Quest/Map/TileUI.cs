using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map
{
    public static class TileUI
    {
        /* 
           [AVAILABLE SQUARE EMOJIS]
           ─────────────────────────────
           ⬛	U+2B1B	Black Large Square
           ⬜	U+2B1C	White Large Square
           🟥	U+1F7E5	Red Square
           🟧	U+1F7E7	Orange Square
           🟨	U+1F7E8	Yellow Square
           🟩	U+1F7E9	Green Square
           🟦	U+1F7E6	Blue Square
           🟪	U+1F7EA	Purple Square
           🟫	U+1F7EB	Brown Square
           ───────────────────────────── */

        /// <summary>
        /// Maps tile type names (string identifiers) to their corresponding emoji representations.
        /// Used to visually display maps in Discord embeds or text output.
        /// </summary>
        private static readonly Dictionary<string, string> EmojiMap = new()
        {
            { "Wall", "⬛" }, // 
            { "Floor", "⬜" },
            { "Grass", "🟩" },
            { "Dirt", "🟫" },
            { "Sand", "🟨" },
            { "Lava", "🟧" },
            { "Water", "🟦" },
            { "ENEMY", "💀" },
            { "Portal", "🌀" },
            { "Treasure", "💰" },
            { "NPC", "🧍" },
            { "Door", "🚪" },
            { "START", "🧍" },
            { "PLAYER", "🧍" }
        };

        /// <summary>
        /// Converts a 2D list of tile identifiers into a multiline string of emojis,
        /// Producing a visual map layout for display in text-based interfaces.
        /// </summary>
        /// <param name="grid">The 2D grid representing tile identifiers.</param>
        /// <returns>A formatted string representation of the grid using emojis.</returns>
        public static string RenderTileGrid(List<List<string>> grid)
        {
            // Ensure grid data exists before rendering
            if (grid == null || grid.Count == 0)
                return "⚠️ Grid data missing";

            var sb = new StringBuilder();

            // Loop through each row in the grid
            foreach (var row in grid)
            {
                // Loop through each cell in the row
                foreach (var cell in row)
                {
                    // Try to find a matching emoji in the map
                    if (EmojiMap.TryGetValue(cell, out var emoji))
                        sb.Append(emoji);
                    else
                        sb.Append("❓"); // Use a question mark if the tile type is unknown
                        //sb.Append("❌"); Use an X if the tile type is unknown
                }

                sb.AppendLine(); // Move to the next row visually
            }

            // Trim any trailing newline characters and return the final grid view
            return sb.ToString().TrimEnd();
        }
    }
}
