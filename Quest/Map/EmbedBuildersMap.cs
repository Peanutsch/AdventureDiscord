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

        public static string Label(string direction) => direction switch
        {
            "West" => "⬅️",
            "North" => "⬆️",
            "South" => "⬇️",
            "East" => "➡️",
            _ => direction
        };

        public static ComponentBuilder BuildDirectionButtons(TileModel tile)
        {
            LogService.DividerParts(1, "BuildDirectionButtons");
            var builder = new ComponentBuilder();

            // --- Default disabled buttons ---
            var buttons = new List<ButtonBuilder>
    {
        new ButtonBuilder().WithLabel("Enter").WithCustomId("enter:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("⬆️").WithCustomId("move_north:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("⬅️").WithCustomId("move_west:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("⬇️").WithCustomId("move_south:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true),
        new ButtonBuilder().WithLabel("➡️").WithCustomId("move_east:none").WithStyle(ButtonStyle.Secondary).WithDisabled(true)
    };

            // --- Enable movement buttons op basis van tile connections ---
            if (tile.Connections != null)
            {
                foreach (var targetTileId in tile.Connections)
                {
                    if (!TestHouseLoader.TileLookup.TryGetValue(targetTileId, out var targetTile)) continue;

                    // Gebruik MapService om richting te bepalen
                    string? dir = MapService.DetermineDirection(tile, targetTile);
                    if (dir == null) continue;

                    int index = dir switch
                    {
                        "North" => 1,
                        "West" => 2,
                        "South" => 3,
                        "East" => 4,
                        _ => -1
                    };

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

            // --- Enable Enter button alleen als huidige tile een DOOR is ---
            if (tile.TileType.Equals("DOOR", StringComparison.OrdinalIgnoreCase) && tile.Connections?.Count > 0)
            {
                // connection uit detail ("small_pond:DOOR") → vind de echte TileId in TileLookup
                string connectionRef = tile.Connections[0]; // "small_pond:DOOR"
                var parts = connectionRef.Split(':'); // ["small_pond", "DOOR"]
                string areaId = parts[0];
                string detailId = parts[1];

                // Zoek tile in TileLookup van dat areaId en detailId
                var targetTile = TestHouseLoader.AreaLookup[areaId].Tiles
                    .FirstOrDefault(t => t.TileType.Equals(detailId, StringComparison.OrdinalIgnoreCase));

                if (targetTile != null)
                {
                    buttons[0] = new ButtonBuilder()
                        .WithLabel("Enter")
                        .WithCustomId($"enter:{targetTile.TileId}") // echte tileId
                        .WithStyle(ButtonStyle.Success)
                        .WithDisabled(false);
                }
            }

            // --- Voeg buttons toe aan builder ---
            builder.WithButton(buttons[0], row: 0)
                   .WithButton(buttons[1], row: 0)
                   .WithButton("Break", "btn_flee", ButtonStyle.Danger, row: 0);

            builder.WithButton(buttons[2], row: 1)
                   .WithButton(buttons[3], row: 1)
                   .WithButton(buttons[4], row: 1);

            LogService.DividerParts(2, "BuildDirectionButtons");
            return builder;
        }
        #endregion

        #region === Embed Builders ===

        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersMap.EmbedWalk] Building embed...");

            var area = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var foundArea)
                       ? foundArea
                       : new TestHouseAreaModel { Name = "Unknown Room", Description = "No description available." };

            string gridVisual = TileUI.RenderTileGrid(tile);
            string tileTextSafe = string.IsNullOrWhiteSpace(tile.TileText)
                ? "<Fallback Text from EmbedBuilder>\nNothing to report..."
                : tile.TileText;

            string exitInfo = (tile.Connections != null && tile.Connections.Count > 0)
                ? string.Join("\n", tile.Connections.Select(conn =>
                {
                    if (!TestHouseLoader.TileLookup.TryGetValue(conn, out var t)) return conn;
                    var dir = MapService.DetermineDirection(tile, t) ?? "Unknown";
                    var targetAreaName = TestHouseLoader.AreaLookup.TryGetValue(t.AreaId, out var areaT) ? areaT.Name : t.AreaId;
                    return $"{dir} → {targetAreaName} ({t.TileId})";
                }))
                : "None";

            LogService.Info($"\nArea: {area.Name}\nDescription:\n{area.Description}\nGrid:\n{gridVisual}\nTile Text:\n{tileTextSafe}\nExits:\n{exitInfo}\nCurrent Tile: {tile.TileId}");

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
