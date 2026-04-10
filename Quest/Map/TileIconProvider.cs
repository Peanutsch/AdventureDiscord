using Adventure.Models.Map;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Responsible for determining and providing tile icons/emojis.
    /// Encapsulates emoji mapping and icon selection logic.
    /// </summary>
    public class TileIconProvider
    {
        private readonly Dictionary<string, string> _emojiMap;

        public TileIconProvider()
        {
            _emojiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Ground / Walls
                { "Wall", "⬛" },
                { "Floor", "⬜" },
                { "Grass", "🟩" },
                { "Dirt", "🟫" },
                { "Sand", "🟨" },
                { "Lava", "🟧" },
                { "Water", "🟦" },
                { "Tree", "🌳" }, // Tree tile with 15% chance NPC Beast attack
                { "Tree2", "🌳" }, // Tree tile with 15% chance NPC Humanoid or Beast attack
                { "BLOCKt", "🌳" }, // Blocked tree tile
                { "BLOCKb", " ⠀  " }, // Blocked blank space
                // Passage
                { "DOOR", "🚪" },
                { "PORTAL", "🌀" },
                { "ExitUp", "⬆️" },
                { "ExitLeft", "⬅️" },
                { "ExitDown", "⬇️" },
                { "ExitRight", "➡️" },
                { "EXIT", "      " }, // Blank space
                // Objects
                { "MONEY", "💰" },
                { "COIN", "🪙" },
                { "PLANT1", "🪴" },
                { "CHEST", "🧰" },
                // Characters
                { "PLAYER", "🧍" },
                {"OTHERPLAYER", "👤" },
                { "ENEMY", "⚔️" },
                { "NPCFEM", "👩" },
                // POI's
                { "START", "🧍" },
                { "ISLAND", "🏝️" }
            };
        }

        /// <summary>
        /// Gets the emoji for a given tile type key.
        /// </summary>
        public string GetEmoji(string key)
        {
            return _emojiMap.TryGetValue(key, out var emoji) ? emoji : "❓";
        }

        /// <summary>
        /// Determines which emoji/icon should be shown for a given grid position.
        /// Respects priority: player > other players > tile overlay > tile base > layout type.
        /// </summary>
        public string GetTileIcon(TestHouseAreaModel area, int row, int col, int playerRow, int playerCol, HashSet<(int Row, int Col)>? otherPlayerPositions = null)
        {
            // Player always takes priority
            if (row == playerRow && col == playerCol)
                return GetEmoji("PLAYER");

            // Other active players shown with a distinct icon
            if (otherPlayerPositions != null && otherPlayerPositions.Contains((row, col)))
                return GetEmoji("OTHERPLAYER");

            // Try to locate tile details for this grid position
            var tileDetail = area.Tiles.FirstOrDefault(t => t.TilePosition == $"{row},{col}");
            string layoutType = area.Layout[row][col];

            // Determine which key to use: overlay > base > layout type
            string? key = null;
            
            if (tileDetail != null)
            {
                key = !string.IsNullOrWhiteSpace(tileDetail.TileOverlay)
                    ? tileDetail.TileOverlay
                    : !string.IsNullOrWhiteSpace(tileDetail.TileBase)
                        ? tileDetail.TileBase
                        : null;
            }

            // Fallback to layout type if no detail or detail has no key
            key ??= layoutType;

            return GetEmoji(key);
        }
    }
}
