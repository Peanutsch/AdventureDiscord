using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Map
{
    public class EmbedBuildersMap
    {
        #region === Buttons ===
        /// <summary>
        /// Handles the creation of Discord component buttons (Enter, Move, Break)
        /// based on the player's current tile and available connections.
        /// </summary>
        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");
            var builder = new ComponentBuilder();

            // Create default disabled buttons
            var buttons = CreateDefaultButtons();

            // Enable movement buttons based on available connections
            EnableMovementButtons(tile, buttons);

            // Enable "Enter" button if current tile is a DOOR
            EnableEnterButton(tile, buttons);

            // Add all buttons to the Discord component builder
            AddButtonsToBuilder(builder, buttons);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }
        #endregion

        #region === Button Helper Methods ===
        /// <summary>
        /// Find tile by:
        /// TileId (like area:EXIT1) or TilePosition (like area:7,8).
        /// </summary>
        private static TileModel? FindTileByIdOrPosition(string areaId, string detailId)
        {
            if (!TestHouseLoader.AreaLookup.TryGetValue(areaId, out var area))
                return null;

            // 1Try to match by TileType
            var tile = area.Tiles.FirstOrDefault(t =>
                t.TileType.Equals(detailId, StringComparison.OrdinalIgnoreCase));

            if (tile != null)
                return tile;

            // If not found, try to match by tile position
            if (detailId.Contains(','))
            {
                string pos = detailId.Trim();
                tile = area.Tiles.FirstOrDefault(t =>
                    t.TilePosition.Equals(pos, StringComparison.OrdinalIgnoreCase));
            }

            return tile;
        }

        /// <summary>
        /// Creates a list of default disabled buttons for Enter, Move (N/W/S/E) and Break.
        /// </summary>
        private static List<ButtonBuilder> CreateDefaultButtons()
        {
            return new List<ButtonBuilder>
    {
        new ButtonBuilder().WithLabel("Enter").WithCustomId("enter:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("⬆️").WithCustomId("move_north:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("⬅️").WithCustomId("move_west:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("⬇️").WithCustomId("move_south:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("➡️").WithCustomId("move_east:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true)
    };
        }

        /// <summary>
        /// Enables directional movement buttons based on tile connections.
        /// </summary>
        /// <param name="tile">The current tile model containing connections.</param>
        /// <param name="buttons">The list of button builders to update.</param>
        private static void EnableMovementButtons(TileModel tile, List<ButtonBuilder> buttons)
        {
            if (tile.Connections == null)
                return;

            foreach (var targetTileId in tile.Connections)
            {
                // Try to find the connected tile in the global lookup
                if (!TestHouseLoader.TileLookup.TryGetValue(targetTileId, out var targetTile))
                    continue;

                // Determine the direction from the current tile to the target tile
                string? dir = MapService.DetermineDirection(tile, targetTile);
                if (dir == null)
                    continue;

                // Map direction to index position in the button list
                int index = dir switch
                {
                    "North" => 1,
                    "West" => 2,
                    "South" => 3,
                    "East" => 4,
                    _ => -1
                };

                // Replace the default disabled button with an active movement button
                if (index >= 0)
                {
                    buttons[index] = new ButtonBuilder()
                        .WithLabel(Label(dir))
                        .WithCustomId($"move:{targetTile.TileId}") // Unique ID for component interaction
                        .WithStyle(ButtonStyle.Primary)
                        .WithDisabled(false);
                }
            }
        }

        /// <summary>
        /// Enables the "Enter" button if the current tile represents a door or exit
        /// with at least one valid connection.
        /// </summary>
        private static void EnableEnterButton(TileModel tile, List<ButtonBuilder> buttons)
        {
            // Check if the tile type is either a DOOR or any variant of EXIT (e.g., EXIT1, EXIT2)
            if (!(tile.TileType.Equals("DOOR", StringComparison.OrdinalIgnoreCase) ||
                  tile.TileType.StartsWith("EXIT", StringComparison.OrdinalIgnoreCase)))
                return;

            // Must have at least one valid connection
            if (tile.Connections == null || tile.Connections.Count == 0)
                return;

            // Example connection: "living_room:EXIT1" or "living_room:7,8"
            string connectionRef = tile.Connections[0];
            var parts = connectionRef.Split(':');
            if (parts.Length != 2)
                return;

            string areaId = parts[0];
            string detailId = parts[1];

            // Try to find the target tile by its ID or position
            var targetTile = FindTileByIdOrPosition(areaId, detailId);

            if (targetTile != null)
            {
                // Enable the Enter button linking to the target tile
                buttons[0] = new ButtonBuilder()
                    .WithLabel("Enter")
                    .WithCustomId($"enter:{targetTile.TileId}")
                    .WithStyle(ButtonStyle.Success)
                    .WithDisabled(false);
            }
        }

        /// <summary>
        /// Adds the prepared buttons to the Discord ComponentBuilder with specific layout rows.
        /// </summary>
        private static void AddButtonsToBuilder(ComponentBuilder builder, List<ButtonBuilder> buttons)
        {
            // Row 0: Enter, North, Break
            builder.WithButton(buttons[0], row: 0)
                   .WithButton(buttons[1], row: 0)
                   .WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0);

            // Row 1: West, South, East
            builder.WithButton(buttons[2], row: 1)
                   .WithButton(buttons[3], row: 1)
                   .WithButton(buttons[4], row: 1);
        }

        /// <summary>
        /// Returns a formatted label string for directional buttons with emoji.
        /// </summary>
        /// <param name="dir">The direction name.</param>
        /// <returns>A readable label for the button.</returns>
        private static string Label(string dir) => dir switch
        {
            "North" => "⬆️",
            "West" => "⬅️",
            "South" => "⬇️",
            "East" => "➡️",
            _ => dir
        };
        #endregion

        #region === Embed Builders ===
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersMap.EmbedWalk] Building embed...");

            var area = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var foundArea)
                       ? foundArea
                       : new TestHouseAreaModel { Name = "Unknown Area", Description = "No description available." };

            LogService.DividerParts(1, "TileUI.RenderTileGrid");
            string gridVisual = TileUI.RenderTileGrid(tile);
            LogService.DividerParts(2, "TileUI.RenderTileGrid");

            string tileTextSafe = string.IsNullOrWhiteSpace(tile.TileText)
                ? "<Fallback Text from EmbedBuilder>\nNothing to report..."
                : tile.TileText;

            string exitInfo = (tile.Connections != null && tile.Connections.Count > 0)
                ? string.Join("\n", tile.Connections.Select(conn =>
                {
                    if (!TestHouseLoader.TileLookup.TryGetValue(conn, out var t)) return conn;
                    var dir = MapService.DetermineDirection(tile, t) ?? "Travel";
                    var targetAreaName = TestHouseLoader.AreaLookup.TryGetValue(t.AreaId, out var areaT) ? areaT.Name : t.AreaId;
                    return $"{dir} → {targetAreaName} ({t.TileId})";
                }))
                : "None";

            LogService.Info($"\n[Area]\n{area.Name}\n[Description]\n{area.Description}\n[Location]\n{tile.TileId}\n[Tile Text]\n{tileTextSafe}\n[Exits]\n{exitInfo}\n[Current Tile]\n{tile.TileId}");

            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[{area.Name}]", area.Description)
                .AddField($"{gridVisual}\n", $"*{tileTextSafe}*")
                .AddField("[Possible Directions]", exitInfo)
                .AddField("[Current Tile]", tile.TileId)
                .AddField("[Tile Position]", tile.TilePosition);
        }
        #endregion
    }
}
