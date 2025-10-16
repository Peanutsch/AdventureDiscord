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
                "West" => "West",
                "North" => "North",
                "South" => "South",
                "East" => "East",
                _ => direction

                /*
                "West" => "⬅️ West",
                "North" => "⬆️ North",
                "South" => "⬇️ South",
                "East" => "➡️ East",
                _ => direction
                */
            };

            return label;
        }

        /// <summary>
        /// Builds Discord buttons for the available exits on the tile.
        /// Placeholder buttons ("blocked") are replaced by actual exits if present.
        /// Row 0: West/East, Row 1: North/South, Break button always row 2.
        /// </summary>
        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");

            var builder = new ComponentBuilder();


            // --- Create a list of placeholders in fixed positions ---
            var buttons = new List<ButtonBuilder>
            {
                new ButtonBuilder().WithLabel("North").WithCustomId("blocked_north").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 0, col 1
                new ButtonBuilder().WithLabel("West").WithCustomId("blocked_west").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 1
                new ButtonBuilder().WithLabel("South").WithCustomId("blocked_south").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 2
                new ButtonBuilder().WithLabel("East").WithCustomId("blocked_east").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 3
            };


            /* Backup of component list 
             * // --- Create a list of placeholders in fixed positions ---
            var buttons = new List<ButtonBuilder>
            {
                new ButtonBuilder().WithLabel("⬅️ West").WithCustomId("blocked_west").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 0, col 0
                new ButtonBuilder().WithLabel("➡️ East").WithCustomId("blocked_east").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 0, col 1
                new ButtonBuilder().WithLabel("⬆️ North").WithCustomId("blocked_north").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 0
                new ButtonBuilder().WithLabel("⬇️ South").WithCustomId("blocked_south").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 1
            };
            */

            var exits = MapService.GetExits(tile, MapLoader.TileLookup);

            // --- Replace placeholders with actual exits if they exist ---
            if (exits.TryGetValue("North", out var north) && !string.IsNullOrEmpty(north))
                buttons[0] = new ButtonBuilder().WithLabel(Label("North")).WithCustomId($"move_north:{north}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("West", out var west) && !string.IsNullOrEmpty(west))
                buttons[1] = new ButtonBuilder().WithLabel(Label("West ")).WithCustomId($"move_west:{west}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("East", out var east) && !string.IsNullOrEmpty(east))
                buttons[2] = new ButtonBuilder().WithLabel(Label("East ")).WithCustomId($"move_east:{east}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("South", out var south) && !string.IsNullOrEmpty(south))
                buttons[3] = new ButtonBuilder().WithLabel(Label("South")).WithCustomId($"move_south:{south}").WithStyle(ButtonStyle.Primary);

            // --- Add buttons to builder with proper rows ---
            builder.WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0);
            builder.WithButton(buttons[0], row: 0); // Button North
            builder.WithButton(buttons[1], row: 1); // Button West
            builder.WithButton(buttons[3], row: 1); // Button South
            builder.WithButton(buttons[2], row: 1); // Button East
            

            // --- Break button always at bottom ---
            //builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 0);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }
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