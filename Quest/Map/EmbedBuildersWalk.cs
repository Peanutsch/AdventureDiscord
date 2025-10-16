using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map
{
    public class EmbedBuildersWalk
    {
        #region === Buttons ===
        /// <summary>
        /// Returns the label for a directional button using emojis.
        /// </summary>
        /// <param name="direction">The direction (North, South, East, West)</param>
        /// <returns>Formatted string with emoji and direction</returns>
        public static string Label(string direction)
        {
            string label = direction switch
            {
                "West" => "⬅️ West",
                "North" => "⬆️ North",
                "South" => "⬇️ South",
                "East" => "➡️ East",
                _ => direction
            };

            return label;
        }

        /// <summary>
        /// Builds Discord buttons for the available exits on the tile.
        /// Placeholder buttons with dummy IDs are added first and then replaced by actual exits if present.
        /// Row 0: West/East, Row 1: North/South, Break button always row 2
        /// </summary>
        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");

            var builder = new ComponentBuilder();

            // --- Add placeholders first ---
            builder.WithButton(Label("West"), "blocked_west", ButtonStyle.Secondary, row: 0);
            builder.WithButton(Label("East"), "blocked_east", ButtonStyle.Secondary, row: 0);
            builder.WithButton(Label("North"), "blocked_north", ButtonStyle.Secondary, row: 1);
            builder.WithButton(Label("South"), "blocked_south", ButtonStyle.Secondary, row: 1);

            var exits = MapService.GetExits(tile, MapLoader.TileLookup);

            // --- Replace placeholders with actual exits if they exist ---
            if (exits.TryGetValue("West", out var west) && !string.IsNullOrEmpty(west))
                builder.WithButton(Label("West"), $"move_west:{west}", ButtonStyle.Primary, row: 0);

            if (exits.TryGetValue("East", out var east) && !string.IsNullOrEmpty(east))
                builder.WithButton(Label("East"), $"move_east:{east}", ButtonStyle.Primary, row: 0);

            if (exits.TryGetValue("North", out var north) && !string.IsNullOrEmpty(north))
                builder.WithButton(Label("North"), $"move_north:{north}", ButtonStyle.Primary, row: 1);

            if (exits.TryGetValue("South", out var south) && !string.IsNullOrEmpty(south))
                builder.WithButton(Label("South"), $"move_south:{south}", ButtonStyle.Primary, row: 1);

            // --- Break button always at bottom ---
            builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }

        #region //--- Test Methods
        /*
        /// <summary>
        /// Builds Discord buttons for the available exits on the tile.
        /// Placeholder buttons ("BLOCKED") are added first and then replaced by actual exits if present.
        /// Row 0: West/East, Row 1: North/South, Break button always row 2
        /// </summary>
        public static ComponentBuilder? BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");

            if (tile.TileGrid == null)
            {
                LogService.Error("[EmbedBuildersWalk.BuildDirectionButtons] tile.TileGrid = null...");
                return null;
            }

            var builder = new ComponentBuilder();
            var exits = MapService.GetExits(tile, MapLoader.TileLookup);

            // Row 0: West / East
            string[] row0Dirs = { "West", "East" };
            foreach (var dir in row0Dirs)
            {
                if (exits.TryGetValue(dir, out var dest) && !string.IsNullOrEmpty(dest))
                    builder.WithButton(Label(dir), $"move_{dir.ToLower()}:{dest}", ButtonStyle.Primary, row: 0);
                else
                    builder.WithButton(Label(dir), "", ButtonStyle.Secondary, row: 0); // placeholder grijs
            }

            // Row 1: North / South
            string[] row1Dirs = { "North", "South" };
            foreach (var dir in row1Dirs)
            {
                if (exits.TryGetValue(dir, out var dest) && !string.IsNullOrEmpty(dest))
                    builder.WithButton(Label(dir), $"move_{dir.ToLower()}:{dest}", ButtonStyle.Primary, row: 1);
                else
                    builder.WithButton(Label(dir), "", ButtonStyle.Secondary, row: 1); // placeholder grijs
            }

            // Break button
            builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);

            LogService.DividerParts(2, "BuildDirectionButtonsWithPlaceholders");
            return builder;
        }
        */

        /*
        /// <summary>
        /// Builds Discord buttons for the available exits on the tile.
        /// West/East buttons are placed on row 0, North/South on row 1, and a Break button on row 2.
        /// </summary>
        /// <param name="tile">The TileModel representing the current location.</param>
        /// <returns>A ComponentBuilder with direction buttons, or null if TileGrid is missing.</returns>
        public static ComponentBuilder? BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");

            if (tile.TileGrid == null)
            {
                LogService.Error("[EmbedBuildersWalk.BuildDirectionButtons] tile.TileGrid = null...");
                return null;
            }

            var builder = new ComponentBuilder();
            var exits = MapService.GetExits(tile, MapLoader.TileLookup);

            // Row 0: West/East
            string[] row0 = { "West", "East" };
            foreach (var dir in row0)
            {
                if (exits.TryGetValue(dir, out var destination) && !string.IsNullOrEmpty(destination))
                {
                    builder.WithButton(Label(dir), $"move_{dir.ToLower()}:{destination}", ButtonStyle.Primary, row: 0);
                    LogService.Info($"Button for {dir} -> {destination} added at row 0");
                }
            }

            // Row 1: North/South
            string[] row1 = { "North", "South" };
            foreach (var dir in row1)
            {
                if (exits.TryGetValue(dir, out var destination) && !string.IsNullOrEmpty(destination))
                {
                    builder.WithButton(Label(dir), $"move_{dir.ToLower()}:{destination}", ButtonStyle.Primary, row: 1);
                    LogService.Info($"Button for {dir} -> {destination} added at row 1");
                }
            }

            // Break button always at bottom row
            builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }
        */
        #endregion --- Test Methods
        #endregion

        #region === Embed Builders ===
        /// <summary>
        /// Builds an embed representing the tile.
        /// Includes:
        /// - Tile grid visualization
        /// - Tile description
        /// - List of possible exits
        /// </summary>
        /// <param name="tile">The TileModel to render</param>
        /// <returns>Discord EmbedBuilder with tile information</returns>
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Building embed...");

            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Get exits...");
            var exits = MapService.GetExits(tile, MapLoader.TileLookup);
            var exitInfo = new StringBuilder();

            foreach (var (exit, destination) in exits!)
            {
                LogService.Info($"{exit} leads to {destination}");
                exitInfo.AppendLine($"**{exit}** leads to **{destination}**");
            }

            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Get gridVisual...");
            
            var gridVisual = TileUI.RenderTileGrid(tile.TileGrid);
            
            LogService.Info($"Grid rendered:\n" +
                            $"{gridVisual}");

            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[You are on tile *{tile.TileName}*]", $"{gridVisual}\n*{tile.TileText}*")
                .AddField($"[Possible Directions]", $"{exitInfo}");

            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Returning embed...");
            return embed;
        }
        #endregion
    }
}
