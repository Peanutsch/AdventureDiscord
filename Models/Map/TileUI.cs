using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Map
{
    public static class TileUI
    {
        // Mapping van tile-types naar emoji's
        private static readonly Dictionary<string, string> EmojiMap = new()
        {
            { "Wall", "⬛" },
            { "Floor", "⬜" },
            { "PLAYER", "🧍" },
            { "Enemy", "🟥" },
            { "Water", "🟦" },
            { "Treasure", "💰" },
            { "NPC", "🧍" },
            { "Door", "🚪" },
            { "START", "🧍" }
        };

        /// <summary>
        /// Converteert een TileGrid naar een string met emoji's per regel.
        /// </summary>
        public static string RenderTileGrid(List<List<string>> grid)
        {
            if (grid == null || grid.Count == 0)
                return "⚠️ Grid data missing";

            var sb = new StringBuilder();

            foreach (var row in grid)
            {
                foreach (var cell in row)
                {
                    if (EmojiMap.TryGetValue(cell, out var emoji))
                        sb.Append(emoji);
                    else
                        sb.Append("❓"); // onbekende tile
                }

                sb.AppendLine(); // nieuwe regel voor elke rij
            }

            return sb.ToString().TrimEnd();
        }
    }
}
