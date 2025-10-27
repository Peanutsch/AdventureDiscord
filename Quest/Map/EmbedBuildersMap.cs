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
        /// Returns the emoji label for a directional button.
        /// </summary>
        /// <param name="direction">Direction: "North", "South", "East", "West"</param>
        /// <returns>Emoji string representing the direction</returns>
        public static string Label(string direction)
        {
            return direction switch
            {
                "West" => "⬅️",
                "North" => "⬆️",
                "South" => "⬇️",
                "East" => "➡️",
                _ => direction
            };
        }

        /// <summary>
        /// Builds Discord buttons for available exits on the tile.
        /// Uses the updated tilePosition-based connections.
        /// </summary>
        /// <param name="tile">Current tile model</param>
        /// <returns>ComponentBuilder with buttons for each exit</returns>
        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");

            var builder = new ComponentBuilder();

            // --- Placeholder buttons (disabled by default) ---
            var buttons = new List<ButtonBuilder>
        {
            new ButtonBuilder().WithLabel("Enter").WithCustomId("blocked_enter").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
            new ButtonBuilder().WithLabel("⬆️").WithCustomId("blocked_north").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
            new ButtonBuilder().WithLabel("⬅️").WithCustomId("blocked_west").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
            new ButtonBuilder().WithLabel("⬇️").WithCustomId("blocked_south").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
            new ButtonBuilder().WithLabel("➡️").WithCustomId("blocked_east").WithStyle(ButtonStyle.Secondary).WithDisabled(true)
        };

            // --- Get exits for this tile ---
            var exits = MapService.GetExits(tile);

            // Map directional buttons automatically based on exits
            foreach (var exit in exits)
            {
                // exit.Key = targetAreaId, exit.Value = targetTileId
                var direction = DetermineDirection(tile, exit.Value); // helper to decide N/S/E/W if needed
                if (direction == null) continue;

                var index = direction switch
                {
                    "North" => 1,
                    "West" => 2,
                    "South" => 3,
                    "East" => 4,
                    _ => -1
                };

                if (index >= 0)
                    buttons[index] = new ButtonBuilder()
                        .WithLabel(Label(direction))
                        .WithCustomId($"move_{direction.ToLower()}:{exit.Key}:{exit.Value}") // include target area + tile
                        .WithStyle(ButtonStyle.Primary);
            }

            // --- Enable "Enter" button if there is at least one connection ---
            if (tile.Connections != null && tile.Connections.Count > 0)
            {
                var firstConnection = tile.Connections[0]; // format: areaId:tilePosition
                buttons[0] = new ButtonBuilder()
                    .WithLabel("Enter")
                    .WithCustomId($"enter:{firstConnection}")
                    .WithStyle(ButtonStyle.Success);
            }

            // --- Add buttons to ComponentBuilder ---
            // Row 0: Enter + North + optional break button
            builder.WithButton(buttons[0], row: 0)
                   .WithButton(buttons[1], row: 0)
                   .WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0);

            // Row 1: West, South, East
            builder.WithButton(buttons[2], row: 1)
                   .WithButton(buttons[3], row: 1)
                   .WithButton(buttons[4], row: 1);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }

        /// <summary>
        /// Determines the direction of a target tile relative to the current tile.
        /// Optional: implement logic based on TilePosition to decide North/South/East/West.
        /// </summary>
        /// <param name="tile">Current tile</param>
        /// <param name="targetTileId">Target tile ID (e.g., "1_1" or "START")</param>
        /// <returns>Direction as string ("North", "South", "East", "West") or null if unknown</returns>
        private static string? DetermineDirection(TileModel tile, string targetTileId)
        {
            // If the current tile has TilePosition in "row,col" format
            if (string.IsNullOrWhiteSpace(tile.TilePosition) || string.IsNullOrWhiteSpace(targetTileId))
                return null;

            var parts = tile.TilePosition.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col))
                return null;

            // For START tile or unknown format, return null (button may just be "Enter")
            if (targetTileId.Equals("START", StringComparison.OrdinalIgnoreCase))
                return null;

            var targetParts = targetTileId.Split('_');
            if (targetParts.Length != 2 || !int.TryParse(targetParts[0], out int targetRow) || !int.TryParse(targetParts[1], out int targetCol))
                return null;

            // Determine direction
            if (targetRow < row) return "North";
            if (targetRow > row) return "South";
            if (targetCol < col) return "West";
            if (targetCol > col) return "East";

            return null; // same tile or unknown
        }
        #endregion

        #region === Embed Builders ===
        /// <summary>
        /// Builds a Discord embed representing a tile, including grid visualization,
        /// tile description, and possible exits. Uses TileModel wrapper for rendering.
        /// </summary>
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersMap.EmbedWalk] Building embed...");

            // --- Retrieve the area this tile belongs to ---
            var area = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var foundArea)
                       ? foundArea
                       : new TestHouseAreaModel
                       {
                           Name = "Unknown Room",
                           Description = "No description available."
                       };

            // --- Area name and description ---
            var areaName = area.Name;
            var areaDescription = area.Description;

            // --- Render the grid using the TileModel wrapper ---
            // This automatically parses tile.TilePosition and places the player emoji
            string gridVisual = TileUI.RenderTileGrid(tile);

            // --- Tile description, safe fallback ---
            string tileTextSafe = string.IsNullOrWhiteSpace(tile.TileText)
                ? "<Fallback Text>\nNothing to report..."
                : tile.TileText;

            // --- Retrieve exits using MapService ---
            var exits = MapService.GetExits(tile);

            // --- Build exit information string for embed ---
            string exitInfo = exits.Any()
                ? string.Join("\n", exits.Select(e =>
                {
                    var targetAreaName = TestHouseLoader.AreaLookup.TryGetValue(e.Key, out var targetArea)
                        ? targetArea.Name
                        : e.Key;
                    return $"**{e.Key}** ({targetAreaName}) → Tile: {e.Value}";
                }))
                : "None";

            // --- Log for debugging ---
            LogService.Info($"\nArea: {areaName}\n" +
                            $"Description:\n{areaDescription}\n" +
                            $"Grid:\n{gridVisual}\n" +
                            $"Tile Text:\n{tileTextSafe}\n" +
                            $"Exits:\n{exitInfo}\n" +
                            $"Current Tile: {tile.TileId}");

            // --- Build and return the Discord embed ---
            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[{areaName}]", areaDescription)
                .AddField($"{gridVisual}\n", $"*{tileTextSafe}*")
                .AddField("[Possible Directions]", exitInfo)
                .AddField("[Current Tile]", tile.TileId)
                .AddField("[Tile Position]", tile.TilePosition);
        }
        #endregion
    }
}
