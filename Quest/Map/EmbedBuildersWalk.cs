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
                /*
                "West" => "West",
                "North" => "North",
                "South" => "South",
                "East" => "East",
                _ => direction
                */

                "West" => "⬅️",
                "North" => "⬆️",
                "South" => "⬇️",
                "East" => "➡️",
                _ => direction
            };

            LogService.Info($"[EmbedbuildersWalk.Label] Direction: {direction}, Lable: {label}");

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
                new ButtonBuilder().WithLabel("Enter").WithCustomId("blocked_btn_placeholder").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 0, col 0
                new ButtonBuilder().WithLabel("⬆️").WithCustomId("blocked_north").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 0, col 1
                new ButtonBuilder().WithLabel("⬅️").WithCustomId("blocked_west").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 1
                new ButtonBuilder().WithLabel("⬇️").WithCustomId("blocked_south").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 2
                new ButtonBuilder().WithLabel("➡️").WithCustomId("blocked_east").WithStyle(ButtonStyle.Secondary).WithDisabled(true), // Row 1, col 3
            };

            var exits = MapService.GetExits(tile, MainHouseLoader.TileLookup);

            // --- Replace placeholders with actual exits if they exist ---
            if (exits.TryGetValue("North", out var north) && !string.IsNullOrEmpty(north))
                buttons[1] = new ButtonBuilder().WithLabel(Label("North")).WithCustomId($"move_north:{north}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("West", out var west) && !string.IsNullOrEmpty(west))
                buttons[2] = new ButtonBuilder().WithLabel(Label("West")).WithCustomId($"move_west:{west}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("South", out var south) && !string.IsNullOrEmpty(south))
                buttons[3] = new ButtonBuilder().WithLabel(Label("South")).WithCustomId($"move_south:{south}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("East", out var east) && !string.IsNullOrEmpty(east))
                buttons[4] = new ButtonBuilder().WithLabel(Label("East")).WithCustomId($"move_east:{east}").WithStyle(ButtonStyle.Primary);

            // --- Add buttons to builder with proper rows ---
            // Row 0
            builder.WithButton(buttons[0], row: 0); // Placeholder / Enter button
            builder.WithButton(buttons[1], row: 0); // Button North
            builder.WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0); // Button Break first, to get button North in "middle"
            // Row 1
            builder.WithButton(buttons[2], row: 1); // Button West
            builder.WithButton(buttons[3], row: 1); // Button South
            builder.WithButton(buttons[4], row: 1); // Button East

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
        /// Builds an embed representing the tile with grid, description, and exits.
        /// Safe: no null or empty values that crash Discord embeds.
        /// </summary>
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Building embed...");

            // --- Room name ---
            var roomName = MainHouseLoader.Rooms
                            .FirstOrDefault(r => r.Value.Contains(tile))
                            .Key ?? "Unknown Room";

            // --- Grid visualization ---
            var gridVisual = TileUI.RenderTileGrid(tile.TileGrid) ?? "No grid available";

            // --- Tile description ---
            var tileTextSafe = string.IsNullOrWhiteSpace(tile.TileText)
                ? "No description available."
                : tile.TileText;

            // --- Exits ---
            var exits = MapService.GetExits(tile, MainHouseLoader.TileLookup);

            var exitInfo = (exits != null && exits.Any())
                ? string.Join("\n", exits.Select(e => $"**{e.Key}** leads to **{e.Value}**"))
                : "None";

            LogService.Info($"\nRoom: {roomName}\n" +
                            $"Grid:\n{gridVisual}\n" +
                            $"TileText:\n{tileTextSafe}\n" +
                            $"Exits:\n{exitInfo}\n");

            // --- Build embed ---
            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[{roomName}]", 
                          $"{gridVisual}\n" +
                          $"*{tileTextSafe}*")
                .AddField("[Possible Directions]", exitInfo);
        }
        #endregion
    }
}
