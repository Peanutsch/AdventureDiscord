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
        public static readonly Dictionary<string, string> EmojiMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // 
            { "Wall", "⬛" },
            { "Floor", "⬜" },
            { "Grass", "🟩" },
            { "Dirt", "🟫" },
            { "Sand", "🟨" },
            { "Lava", "🟧" },
            { "Water", "🟦" },
            //
            { "DOOR", "🚪" },
            { "PORTAL", "🌀" },
            //
            { "TREASURE", "💰" },
            //
            { "PLAYER", "🧍" },
            { "ENEMY", "⚔️" },
            { "NPC", "🧍" },
            //
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

            // Player positie ophalen via ParseTilePosition
            var (playerRow, playerCol) = ParseTilePosition(tile.TilePosition);

            var sb = new StringBuilder();

            for (int row = 0; row < layout.Count; row++)
            {
                for (int col = 0; col < layout[row].Count; col++)
                {
                    // Player neemt prioriteit
                    if (row == playerRow && col == playerCol)
                    {
                        sb.Append(EmojiMap.TryGetValue("PLAYER", out var playerEmoji) ? playerEmoji : "🧍");
                        continue;
                    }

                    string tileType = layout[row][col];
                    LogService.Info($"[TileUI.RenderTileGrid] tileType: {tileType}");

                    // Vind tile details als overlay/base nodig is
                    var tileDetail = area.Tiles.FirstOrDefault(t => t.TilePosition == $"{row},{col}");

                    string icon = "❓"; // default
                    //if (tileDetail != null)
                    if (!string.IsNullOrEmpty(tileDetail!.TileBase)|| !string.IsNullOrEmpty(tileDetail.TileOverlay))
                    {
                        LogService.Info($"[TileUI.RenderTileGrid] Tile Position: ({tileDetail.TilePosition}) TileBase: {tileDetail.TileBase}, TileOverlay: {tileDetail.TileOverlay}");

                        // Gebruik overlay als die aanwezig is, anders base
                        string? key = !string.IsNullOrWhiteSpace(tileDetail.TileOverlay)
                                     ? tileDetail.TileOverlay
                                     : tileDetail.TileBase;

                        LogService.Info($"[TileUI.RenderTileGrid] key: {key}");

                        if (!string.IsNullOrWhiteSpace(key))
                            icon = EmojiMap.TryGetValue(key.ToUpper(), out var emoji) ? emoji : "❓";
                    }
                    else
                    {
                        LogService.Info($"[TileUI.RenderTileGrid] Tile Position: ({tileDetail.TilePosition}) tileBase['{tileDetail.TileBase}'] OR tileOverlay['{tileDetail.TileOverlay}'] empty... Use '{tileType}' to search for icon");
                        icon = EmojiMap.TryGetValue(tileType.ToUpper(), out var emoji) ? emoji : "❓";
                    }

                    LogService.Info($"[TileUI.RenderTileGrid] icon: {icon}");
                    sb.Append(icon);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        #endregion
    }
}