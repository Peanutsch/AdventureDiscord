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
            if (tile.Connections != null && tile.Connections.Count > 0)
            {
                foreach (var targetTileId in tile.Connections)
                {
                    if (!TestHouseLoader.TileLookup.TryGetValue(targetTileId, out var targetTile)) continue;

                    string? dir = targetTile.TileDirectionFrom(tile); // Berekent richting t.o.v. huidige tile
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
                        buttons[index] = new ButtonBuilder()
                            .WithLabel(Label(dir))
                            .WithCustomId($"move:{targetTile.TileId}")
                            .WithStyle(ButtonStyle.Primary)
                            .WithDisabled(false);
                }
            }

            // --- Enable Enter button alleen als huidige tile een DOOR is ---
            if (tile.TileType.Equals("DOOR", System.StringComparison.OrdinalIgnoreCase))
            {
                // Neem eerste verbinding als bestemming
                if (tile.Connections != null && tile.Connections.Count > 0)
                {
                    string firstConnection = tile.Connections[0];
                    if (TestHouseLoader.TileLookup.ContainsKey(firstConnection))
                    {
                        buttons[0] = new ButtonBuilder()
                            .WithLabel("Enter")
                            .WithCustomId($"enter:{firstConnection}")
                            .WithStyle(ButtonStyle.Success)
                            .WithDisabled(false);
                    }
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
    

        // Helper om richting te bepalen tussen twee tiles
        private static string? DetermineDirection(TileModel current, TileModel target)
        {
            if (current.TilePosition == null || target.TilePosition == null) return null;

            var currParts = current.TilePosition.Split(',');
            var targParts = target.TilePosition.Split(',');

            if (currParts.Length != 2 || targParts.Length != 2) return null;

            int currRow = int.Parse(currParts[0]), currCol = int.Parse(currParts[1]);
            int targRow = int.Parse(targParts[0]), targCol = int.Parse(targParts[1]);

            if (targRow < currRow) return "North";
            if (targRow > currRow) return "South";
            if (targCol < currCol) return "West";
            if (targCol > currCol) return "East";

            return null;
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
                    var dir = DetermineDirection(tile, t) ?? "Unknown";
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
