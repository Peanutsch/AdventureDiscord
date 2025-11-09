using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Modules;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Encounter;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map
{
    public class ButtonBuildersMap
    {
        #region === Build Direction Buttons ===
        /// <summary>
        /// Handles the creation of Discord component buttons (Enter, Move, Break)
        /// based on the player's current tile and available connections.
        /// </summary>
        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");
            var builder = new ComponentBuilder();

            // --- Create default disabled buttons --- 
            var buttons = CreateDefaultButtons();

            // --- Enable movement buttons based on available connections --- 
            EnableMovementButtons(tile, buttons);

            // --- Enable "Enter" button if current tile is a DOOR --- 
            EnableActionButton(tile, buttons);

            // --- Add all buttons to the Discord component builder --- 
            AddButtonsToBuilder(builder, buttons);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }

        /// <summary>
        /// Find tile by:
        /// TileId (like area:EXIT1) or TilePosition (like area:7,8).
        /// </summary>
        private static TileModel? FindTileByIdOrPosition(string areaId, string detailId)
        {
            if (!TestHouseLoader.AreaLookup.TryGetValue(areaId, out var area))
                return null;

            // --- Try to match by TileType --- 
            var tile = area.Tiles.FirstOrDefault(t =>
                t.TileType.Equals(detailId, StringComparison.OrdinalIgnoreCase));

            if (tile != null)
                return tile;

            // --- If no TileType found, try to match by tile position --- 
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
                new ButtonBuilder().WithLabel("Action").WithCustomId("enter:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("⬆️").WithCustomId("move_up:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("⬅️").WithCustomId("move_left:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("⬇️").WithCustomId("move_down:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
                new ButtonBuilder().WithLabel("➡️").WithCustomId("move_right:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true)
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
                // --- Try to find the connected tile in the global lookup --- 
                if (!TestHouseLoader.TileLookup.TryGetValue(targetTileId, out var targetTile))
                    continue;

                // --- Determine the direction from the current tile to the target tile --- 
                string? dir = MapService.DetermineDirection(tile, targetTile);
                if (dir == null)
                    continue;

                // --- Map direction to index position in the button list --- 
                int index = dir switch
                {
                    "Up" => 1,
                    "Left" => 2,
                    "Down" => 3,
                    "Right" => 4,
                    _ => -1
                };

                // --- Replace the default disabled button with an active movement button --- 
                if (index >= 0)
                {
                    buttons[index] = new ButtonBuilder()
                        .WithLabel(Label(dir))
                        .WithCustomId($"move:{targetTile.TileId}")
                        .WithStyle(ButtonStyle.Primary)
                        .WithDisabled(false);
                }
            }
        }
        #endregion

        #region === Build Action Button ===
        /// <summary>
        /// Enables the main "Action" button (Enter/Locked) based on the current tile's type,
        /// connections, and lock state. This button may be labeled "Enter" or "LOCKED"
        /// depending on whether the door or exit can be accessed.
        /// </summary>
        private static void EnableActionButton(TileModel tile, List<ButtonBuilder> buttons)
        {
            // --- Ensure this tile is a door or an exit before creating an action button ---
            if (!IsActionEvent(tile))
                return;

            // --- Verify that this tile has at least one valid connection to another tile ---
            if (!HasValidConnection(tile))
                return;

            // --- Retrieve the tile connected to this door or exit ---
            var targetTile = GetTargetTile(tile);
            if (targetTile == null)
                return;

            // --- Determine the label, style, and disabled state for the action button ---
            var (label, style, disabled) = GetActionButtonState(tile);

            // --- Create and assign the button to the first position in the button list ---
            buttons[0] = new ButtonBuilder()
                .WithLabel(label)
                //.WithCustomId($"enter:{targetTile.TileId}")
                .WithStyle(style)
                .WithDisabled(disabled);

            // -- Handle events like Enter door/entrance, fight NPC
            if (tile.TileType.StartsWith("NPC", StringComparison.OrdinalIgnoreCase))
            {
                buttons[0] = new ButtonBuilder()
                    .WithLabel("Fight")
                    .WithCustomId("encounter:npc")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(false);
            }
            else
            {
                buttons[0].WithCustomId($"enter:{targetTile.TileId}");
            }
        }

        /// <summary>
        /// Determines whether the given tile represents a door or an exit.
        /// </summary>
        /// <param name="tile">The current tile being evaluated.</param>
        /// <returns>True if the tile is a door or an exit; otherwise, false.</returns>
        private static bool IsActionEvent(TileModel tile)
        {
            // --- Door: any tile type starting with "DOOR"
            // --- Exit: any tile type starting with "EXIT" (e.g., EXIT1, EXIT2)
            bool result = tile.TileType.StartsWith("DOOR", StringComparison.OrdinalIgnoreCase)
                       || tile.TileType.StartsWith("EXIT", StringComparison.OrdinalIgnoreCase)
                       || tile.TileType.StartsWith("NPC", StringComparison.OrdinalIgnoreCase);

            return result;
        }

        /// <summary>
        /// Checks whether the tile has one or more valid connection references.
        /// </summary>
        /// <param name="tile">The tile being checked.</param>
        /// <returns>True if valid connections exist; otherwise, false.</returns>
        private static bool HasValidConnection(TileModel tile)
        {
            // --- Tile must contain at least one connection to be usable as an entry point ---
            bool valid = tile.Connections != null && tile.Connections.Count > 0;

            if (!valid)
                LogService.Info("[EnableActionButton] tile.Connections == null or 0");

            return valid;
        }

        /// <summary>
        /// Finds the target tile that this door or exit connects to.
        /// </summary>
        /// <param name="tile">The current tile containing a connection reference.</param>
        /// <returns>The connected TileModel if found; otherwise, null.</returns>
        private static TileModel? GetTargetTile(TileModel tile)
        {
            // --- Example connection format: "living_room:EXIT1" or "living_room:7,8" ---
            string connectionRef = tile.Connections![0];
            var parts = connectionRef.Split(':');

            // --- Ensure the reference is valid (areaId:detailId) ---
            if (parts.Length != 2)
                return null;

            string areaId = parts[0];
            string detailId = parts[1];

            // --- Attempt to find the target tile by ID or position ---
            return FindTileByIdOrPosition(areaId, detailId);
        }

        /// <summary>
        /// Determines the correct label, style, and state for the action button
        /// based on the lock status of the current tile.
        /// </summary>
        /// <param name="tile">The current tile being processed.</param>
        /// <returns>
        /// A tuple containing:
        /// - Label: the text shown on the button (e.g., "Enter" or "LOCKED")
        /// - Style: the Discord button style (e.g., Success or Secondary)
        /// - Disabled: whether the button should be interactable
        /// </returns>
        private static (string Label, ButtonStyle Style, bool Disabled) GetActionButtonState(TileModel tile)
        {
            // --- Default: the door or exit is open and can be entered ---
            string label = "Enter";
            var style = ButtonStyle.Success;
            bool disabled = false;

            // --- If the tile has a lock and it's locked, change the button to "LOCKED" ---
            if (tile.LockState?.LockType != string.Empty && tile.LockState?.Locked == true)
            {
                label = "🔒";
                style = ButtonStyle.Danger; // Red color
                disabled = true;            // Button disabled
            }

            // -- I the tile is an enemy, change button to "Fight" ---
            if (tile.TileName.StartsWith("NPC"))
            {
                LogService.Info($"[ButtonBuildersMap.GetActionButtonState] TileName starts with 'NPC':\nTileName {tile.TileName}, Tileype: {tile.TileType}, TileId: {tile.TileId}\n");
                label = "Fight";
                style = ButtonStyle.Danger;
                disabled = false;
            }

            return (label, style, disabled);
        }
        #endregion

        #region === Add Buttons to Builder ===
        /// <summary>
        /// Adds the prepared buttons to the Discord ComponentBuilder with specific layout rows.
        /// </summary>
        private static void AddButtonsToBuilder(ComponentBuilder builder, List<ButtonBuilder> buttons)
        {
            // --- Row 0: Enter, Up, Break --- 
            builder.WithButton(buttons[0], row: 0)
                   .WithButton(buttons[1], row: 0)
                   .WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0);

            // --- Row 1: Left, Down, Right
            builder.WithButton(buttons[2], row: 1)
                   .WithButton(buttons[3], row: 1)
                   .WithButton(buttons[4], row: 1);
        }
        #endregion

        #region === Direction Labels ===
        /// <summary>
        /// Returns a formatted label string for directional buttons with emoji.
        /// </summary>
        /// <param name="dir">The direction name.</param>
        /// <returns>A readable label for the button.</returns>
        private static string Label(string dir) => dir switch
        {
            "Up" => "⬆️",
            "Left" => "⬅️",
            "Down" => "⬇️",
            "Right" => "➡️",
            _ => dir
        };
        #endregion
    }
}
