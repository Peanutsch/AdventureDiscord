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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map
{
    public class EmbedBuildersMap
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
        /// Builds Discord buttons for the available exits on the tile, including "Enter" for connected tiles.
        /// </summary>
        /// <param name="tile">The current tile model.</param>
        /// <returns>A ComponentBuilder with directional and Enter buttons.</returns>
        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");

            var builder = new ComponentBuilder();

            // --- Create placeholder buttons ---
            var buttons = new List<ButtonBuilder>
            {
                new ButtonBuilder().WithLabel("Enter").WithCustomId("blocked_enter").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("⬆️").WithCustomId("blocked_north").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("⬅️").WithCustomId("blocked_west").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("⬇️").WithCustomId("blocked_south").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("➡️").WithCustomId("blocked_east").WithStyle(ButtonStyle.Secondary).WithDisabled(true)
            };

            // --- Get directional exits ---
            var exits = MapService.GetExits(tile, MainHouseLoader.TileLookup);

            if (exits.TryGetValue("North", out var north) && !string.IsNullOrEmpty(north))
                buttons[1] = new ButtonBuilder().WithLabel(Label("North")).WithCustomId($"move_north:{north}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("West", out var west) && !string.IsNullOrEmpty(west))
                buttons[2] = new ButtonBuilder().WithLabel(Label("West")).WithCustomId($"move_west:{west}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("South", out var south) && !string.IsNullOrEmpty(south))
                buttons[3] = new ButtonBuilder().WithLabel(Label("South")).WithCustomId($"move_south:{south}").WithStyle(ButtonStyle.Primary);

            if (exits.TryGetValue("East", out var east) && !string.IsNullOrEmpty(east))
                buttons[4] = new ButtonBuilder().WithLabel(Label("East")).WithCustomId($"move_east:{east}").WithStyle(ButtonStyle.Primary);

            // --- Enable Enter button if there are connections ---
            if (tile.Connections != null && tile.Connections.Count > 0)
            {
                // Use the first item in connection as target
                var targetConnection = tile.Connections[0];
                buttons[0] = new ButtonBuilder().WithLabel("Enter").WithCustomId($"enter:{targetConnection}").WithStyle(ButtonStyle.Success);
            }

            // --- Add buttons to ComponentBuilder ---
            // Row 0
            builder.WithButton(buttons[0], row: 0); // Enter
            builder.WithButton(buttons[1], row: 0); // North
            builder.WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0); // Break button

            // Row 1
            builder.WithButton(buttons[2], row: 1); // West
            builder.WithButton(buttons[3], row: 1); // South
            builder.WithButton(buttons[4], row: 1); // East

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }
        #endregion

        #region ===> old_BuildDirectionButtons <===
        /// <summary>
        /// Builds Discord buttons for the available exits on the tile.
        /// Placeholder buttons ("blocked") are replaced by actual exits if present.
        /// Row 0: West/East, Row 1: North/South, Break button always row 2.
        /// </summary>
        public static ComponentBuilder old_BuildDirectionButtons(TileModel tile)
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
            builder.WithButton(buttons[0], row: 0); // Placeholder / Button Enter
            builder.WithButton(buttons[1], row: 0); // Button North
            builder.WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0); // Button Break
            // Row 1
            builder.WithButton(buttons[2], row: 1); // Button West
            builder.WithButton(buttons[3], row: 1); // Button South
            builder.WithButton(buttons[4], row: 1); // Button East

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }
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

            // --- Room Description ---
            var roomDescription = MainHouseLoader.RoomDescriptions[roomName];

            // --- Grid visualization ---
            var gridVisual = TileUI.RenderTileGrid(tile.TileGrid) ?? "No grid available";

            // --- Tile description ---
            var tileTextSafe = string.IsNullOrWhiteSpace(tile.TileText)
                ? "nothing to report..."
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
                .AddField($"[{roomName}]", $"{roomDescription}")
                .AddField($"{gridVisual}\n",
                          $"*{tileTextSafe}*")
                .AddField("[Possible Directions]", exitInfo);
        }
        #endregion
    }
}
