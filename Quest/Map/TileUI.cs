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
            // Passage
            { "DOOR", "🚪" },
            { "PORTAL", "🌀" },
            { "ExitUp", "⬆️" },
            { "ExitLeft", "⬅️" },
            { "ExitDown", "⬇️" },
            { "ExitRight", "➡️" },
            // Objects
            { "TREASURE", "💰" },
            { "PLANT1", "🪴" },
            { "Tree", "🌳" },
            // Characters
            { "PLAYER", "🧍" },
            { "ENEMY", "⚔️" },
            { "NPC", "🧍" },
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
                    //LogService.Info($"TileType: {tileType}");

                    // Vind tile details als overlay/base nodig is
                    var tileDetail = area.Tiles.FirstOrDefault(t => t.TilePosition == $"{row},{col}");

                    string icon = "❓"; // default
                    if (!string.IsNullOrEmpty(tileDetail!.TileBase)|| !string.IsNullOrEmpty(tileDetail.TileOverlay))
                    {
                        //LogService.Info($"Tile Position: ({tileDetail.TilePosition}) TileBase: {tileDetail.TileBase}, TileOverlay: {tileDetail.TileOverlay}");

                        // Gebruik overlay als die aanwezig is, anders base
                        string? key = !string.IsNullOrWhiteSpace(tileDetail.TileOverlay)
                                     ? tileDetail.TileOverlay
                                     : tileDetail.TileBase;

                        //LogService.Info($"Key: {key}");

                        if (!string.IsNullOrWhiteSpace(key))
                            icon = EmojiMap.TryGetValue(key.ToUpper(), out var emoji) ? emoji : "❓";
                    }
                    else
                    {
                        //LogService.Info($"Tile Position: ({tileDetail.TilePosition}) tileBase['{tileDetail.TileBase}'] OR tileOverlay['{tileDetail.TileOverlay}'] empty... Use '{tileType}' to search for icon");
                        icon = EmojiMap.TryGetValue(tileType.ToUpper(), out var emoji) ? emoji : "❓";
                    }

                    //LogService.Info($"{tileDetail.TilePosition} use icon: {icon}");
                    sb.Append(icon);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        #endregion
    }
}