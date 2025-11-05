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
                ? "\u2800"
                : tile.TileText;

            string exitInfo = (tile.Connections != null && tile.Connections.Count > 0)
                ? string.Join("\n", tile.Connections.Select(conn =>
                {
                    if (!TestHouseLoader.TileLookup.TryGetValue(conn, out var t)) return conn;
                    var dir = MapService.DetermineDirection(tile, t) ?? "Travel";
                    var targetAreaName = TestHouseLoader.AreaLookup.TryGetValue(t.AreaId, out var areaT)
                        ? areaT.Name
                        : t.AreaId;
                    return $"{dir} → {targetAreaName} ({t.TileId})";
                }))
                : "None";

            // --- Toggle lock if the current tile has a switch ---
            if (TestHouseLockService.ToggleLockBySwitch(tile, TestHouseLoader.LockLookup))
            {
                LogService.Info($"[EmbedBuildersMap.EmbedWalk] Tile {tile.TileId} switch toggled lock: {tile.LockId}");
            }

            // === Build embed ===
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[{area.Name}]", area.Description)
                .AddField($"{gridVisual}\n", $"{tileTextSafe}")
                .AddField("[Possible Directions]", exitInfo)
                .AddField("[Current Tile/Type]", $"{tile.TileId}/{tile.TileType}");

            // === Add lock info ===
            if (tile.LockState!.LockType != "---")
            {
                LogService.Info($"LockType is {tile.LockState!.LockType}");
                embed.AddField("[LockType/IsLocked]", $"{tile.LockState.LockType}/{tile.LockState.Locked}");
            }
            else
            {
                LogService.Info($"LockType is empty...");
                embed.AddField("[LockType/IsLocked]", "---/---");
            }

            // === Debug log ===
            LogService.Info($"\n[Area]\n{area.Name}\n" +
                            $"[Description]\n{area.Description}\n" +
                            $"[Location]\n{tile.TileId}\n" +
                            $"[Tile Text]\n{tileTextSafe}\n" +
                            $"[Exits]\n{exitInfo}\n" +
                            $"[Current Tile]\n{tile.TileId}\n" +
                            $"[LockType/IsLocked]\n{tile.LockState?.LockType ?? "---"}/{(tile.LockState?.Locked.ToString().ToLower() ?? "---")}\n");

            return embed;
        }
        #endregion
    }
}
