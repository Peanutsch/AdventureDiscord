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
    public static class TileUI
    {
        #region === Dictionary of used Emojis ===
        public static readonly Dictionary<string, string> EmojiMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Ground / Walls
            { "Wall", "⬛" },
            { "Floor", "⬜" },
            { "Grass", "🟩" },
            { "Dirt", "🟫" },
            { "Sand", "🟨" },
            { "Lava", "🟧" },
            { "Water", "🟦" },
            { "Tree", "🌳" },
            { "BLOCKt", "🌳" },
            //{ "BLOCK", " ⠀  " }, // Braille blank
            // Passage
            { "DOOR", "🚪" },
            { "PORTAL", "🌀" },
            { "ExitUp", "⬆️" },
            { "ExitLeft", "⬅️" },
            { "ExitDown", "⬇️" },
            { "ExitRight", "➡️" },
            { "EXIT", "      " }, // Braille blank
            // Objects
            { "TREASURE", "💰" },
            { "PLANT1", "🪴" },
            // Characters
            { "PLAYER", "🧍" },
            { "ENEMY", "⚔️" },
            { "NPCFEM", "👩" },
            // POI's
            { "START", "🧍" }
        };
        #endregion

        #region === Render Grid ===
        /// <summary>
        /// Converts a TilePosition string "row,col" to row/col integers.
        /// </summary>
        public static (int row, int col) ParseTilePosition(string tilePos)
        {
            var parts = tilePos.Split(',');
            if (int.TryParse(parts[0], out int row) &&
                int.TryParse(parts[1], out int col))
            {
                return (row, col);
            }

            return (-1, -1);
        }

        /// <summary>
        /// Renders a visual grid for the current area with the player icon placed at the correct position.
        /// </summary>
        public static string RenderTileGrid(TileModel tile)
        {
            // 1️⃣ Validate and retrieve the area layout
            if (!TryGetAreaLayout(tile, out var area, out var layout))
                return "<Unknown Area>";

            // 2️⃣ Parse player coordinates
            var (playerRow, playerCol) = ParseTilePosition(tile.TilePosition);

            // 3️⃣ Build the grid row by row
            var sb = new StringBuilder();
            for (int row = 0; row < layout.Count; row++)
            {
                for (int col = 0; col < layout[row].Count; col++)
                {
                    string icon = GetTileIcon(area!, row, col, playerRow, playerCol);
                    sb.Append(icon);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Attempts to retrieve the area and its layout for a given tile.
        /// Returns false if missing or invalid.
        /// </summary>
        private static bool TryGetAreaLayout(TileModel tile, out TestHouseAreaModel? area, out List<List<string>> layout)
        {
            layout = new();
            if (!TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out area))
                return false;

            if (area.Layout == null || area.Layout.Count == 0)
                return false;

            layout = area.Layout;
            return true;
        }

        /// <summary>
        /// Determines which emoji/icon should be shown for a given grid position.
        /// </summary>
        private static string GetTileIcon(TestHouseAreaModel area, int row, int col, int playerRow, int playerCol)
        {
            // Player always takes priority
            if (row == playerRow && col == playerCol)
                return EmojiMap.TryGetValue("PLAYER", out var playerEmoji) ? playerEmoji : "🧍";

            // Try to locate tile details for this grid position
            var tileDetail = area.Tiles.FirstOrDefault(t => t.TilePosition == $"{row},{col}");
            string tileType = area.Layout[row][col];

            // Default icon
            string icon = "❓";

            if (tileDetail != null)
            {
                // Prefer overlay > base > layout type
                string? key = !string.IsNullOrWhiteSpace(tileDetail.TileOverlay)
                             ? tileDetail.TileOverlay
                             : tileDetail.TileBase;

                if (!string.IsNullOrWhiteSpace(key))
                    icon = EmojiMap.TryGetValue(key.ToUpper(), out var emoji) ? emoji : "❓";
                else
                    icon = EmojiMap.TryGetValue(tileType.ToUpper(), out var emoji) ? emoji : "❓";
            }
            else
            {
                // Fallback if no tile detail found
                icon = EmojiMap.TryGetValue(tileType.ToUpper(), out var emoji) ? emoji : "❓";
            }

            return icon;
        }
        #endregion
    }
}