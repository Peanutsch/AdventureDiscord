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
        #region === Possible Emoji's ===
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

        /* 
        ─────────────────────────────
        [Generic / Townsfolk]
       👤 — Generic person
       🧑 — Neutral adult
       🧓 — Elderly person
       👶 — Child
       👩 / 👨 — Adult female/male
       🧑‍🦱 / 🧑‍🦰 — NPC met kapselvariatie
       👴 / 👵 — Old man / old woman
        ─────────────────────────────
        [🏰 Fantasy / RPG Characters]
       🧙‍♂️ / 🧙‍♀️ — Wizard / Sorcerer
       🧝‍♂️ / 🧝‍♀️ — Elf
       🧛‍♂️ / 🧛‍♀️ — Vampire
       🧟‍♂️ / 🧟‍♀️ — Zombie / Undead
       🧞‍♂️ / 🧞‍♀️ — Genie / Spirit
       🧚‍♂️ / 🧚‍♀️ — Fairy / Sprite
       🧜‍♂️ / 🧜‍♀️ — Merman / Mermaid
       🐉 — Dragon / Boss Creature
       🐺 — Wolf / Beast Companion
       🐍 — Snake / Poisonous Creature
       ─────────────────────────────
        [🏹 Warrior / Guard / Soldier]
       🗡️ — Rogue / Assassin
       ⚔️ — Knight / Warrior
       🛡️ — Guard / Protector
       🪓 — Barbarian / Lumberjack-style NPC
       🤺 — Fencer / Duelist
       🏹 — Hunter / Ranger
       ─────────────────────────────
        [🏪 Merchants / Civilians]
       💂‍♂️ / 💂‍♀️ — Guard / Soldier
       🏪 — Shopkeeper / Merchant
       🧵 — Weaver / Merchant
       🍳 — Cook / Innkeeper
       🪙 — Banker / Money handler
       📚 — Scholar / Librarian
       🎭 — Entertainer / Performer
       ─────────────────────────────
        [😄 Emotive / Role-specific]
       🤵 / 👰 — Noble / Lord / Lady
       👮‍♂️ / 👮‍♀️ — Police / Lawkeeper
       🕵️‍♂️ / 🕵️‍♀️ — Detective / Investigator
       🧩 — Quest giver / Puzzle master
       🎨 — Artist / Painter NPC
       🎵 — Bard / Musician
       ─────────────────────────────
        [🐾 Creature / Monster NPCs]
       🐲 — Dragon
       🐺 — Wolf / Beast
       🦁 — Lion / Beast
       🦅 — Bird NPC / Scout
       🐍 — Snake / Poisonous Creature
       🦇 — Bat / Nocturnal Creature
       🐉 — Legendary Boss / Creature
       ─────────────────────────────
        [🌿 Nature / Mystic NPCs]
       🌳 — Forest spirit
       🌲 — Woodland NPC
       🌊 — Water spirit / Mermaid
       🔥 — Fire elemental / Flame NPC
       ❄️ — Ice elemental / Snow NPC
       🌪️ — Wind elemental / Storm NPC
       ─────────────────────────────
        [🪄 Magic / Rare NPCs]
       ✨ — Enchanter / Magical NPC
       🔮 — Seer / Fortune teller
       🕯️ — Mage / Ritual NPC
       📜 — Scholar / Quest giver
       ⚗️ — Alchemist / Potion NPC
        ─────────────────────────────*/
        #endregion

        #region === Dictionary of used Emojis ===
        /// <summary>
        /// Maps tile type identifiers to their emoji representations.
        /// Used to visually render map layouts inside Discord embeds.
        /// </summary>
        private static readonly Dictionary<string, string> EmojiMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "WALL", "⬛" },
            { "FLOOR", "⬜" },
            { "GRASS", "🟩" },
            { "DIRT", "🟫" },
            { "SAND", "🟨" },
            { "LAVA", "🟧" },
            { "WATER", "🟦" },
            { "ENEMY", "👤" },
            { "PORTAL", "🌀" },
            { "TREASURE", "💰" },
            { "NPC", "🧍" },
            { "DOOR", "🚪" },
            { "START", "🧍" },
            { "PLAYER", "🧍" }
        };
        #endregion

        #region === Render Grid ===
        /// <summary>
        /// Converts a TilePosition string "row,col" to row/col integers.
        /// </summary>
        public static (int row, int col) ParseTilePosition(string tilePos)
        {
            var parts = tilePos.Split(',');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int row) &&
                int.TryParse(parts[1], out int col))
            {
                return (row, col);
            }

            return (-1, -1);
        }

        /// <summary>
        /// Renders a grid for the area and places the player at the correct position.
        /// </summary>
        /// <param name="tile">TileModel representing the player's current tile</param>
        /// <returns>String of the rendered grid with player emoji</returns>
        public static string RenderTileGrid(TileModel tile)
        {
            if (!TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var area))
                return "<Unknown Area>";

            var layout = area.Layout;
            if (layout == null || layout.Count == 0)
                return "<No layout>";

            // Parse row,col from tile.TilePosition
            int playerRow = -1, playerCol = -1;
            if (!string.IsNullOrWhiteSpace(tile.TilePosition))
            {
                var parts = tile.TilePosition.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int r) &&
                    int.TryParse(parts[1], out int c))
                {
                    playerRow = r;
                    playerCol = c;
                }
            }

            var sb = new System.Text.StringBuilder();

            for (int row = 0; row < layout.Count; row++)
            {
                for (int col = 0; col < layout[row].Count; col++)
                {
                    // Player takes priority
                    if (row == playerRow && col == playerCol)
                    {
                        sb.Append("🧍");
                        continue;
                    }

                    string tileType = layout[row][col];

                    // Map tileType to emoji
                    string icon = tileType switch
                    {
                        "Wall" => "⬛",
                        "Floor" => "⬜",
                        "Water" => "🟦",
                        "DOOR" => "🚪",
                        "ENEMY" => "👤",
                        "START" => "⬜",
                        _ => "❓"
                    };

                    sb.Append(icon);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        #endregion
    }
}