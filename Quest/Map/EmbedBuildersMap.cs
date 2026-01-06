using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Adventure.Quest.Map
{
    public static class EmbedBuildersMap
    {
        #region === Embed ===
        /// <summary>
        /// Builds the embed that displays the player's current location on the map.
        /// This includes area info, tile description, grid layout, and lock information.
        /// </summary>
        /// <param name="tile">The tile model representing the player's current position.</param>
        /// <returns>An <see cref="EmbedBuilder"/> ready to be sent to Discord.</returns>
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersMap.EmbedWalk] Building embed...");

            // --- Fetch basic area and tile data ---
            var area = GetArea(tile);
            string gridVisual = TileUI.RenderTileGrid(tile);
            string tileTextSafe = GetTileText(tile);
            string exitInfo = BuildExitInfo(tile);

            // --- Handle locks (toggle if tile acts as a switch) ---
            TestHouseLockService.ToggleLockBySwitch(tile, TestHouseLoader.LockLookup);

            // Truncate area name if too long
            string safeAreaName = area.Name.Length > 250
                ? area.Name.Substring(0, 250) + "..."
                : area.Name;

            // --- Build the main embed structure ---
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[{safeAreaName}]", area.Description)
                .AddField($"{gridVisual}\n", $"{tileTextSafe}");

            // --- Add extra info as lock state ---
            if (tile.LockState!.Locked || tile.LockSwitch)
                AddLockInfo(embed, tile);

            // --- Log all collected tile information for debugging ---
            LogTileDebugInfo(tile, area, tileTextSafe, exitInfo);

            return embed;
        }
        #endregion

        #region === Get Area data and TileText ===
        /// <summary>
        /// Retrieves the area object based on the tile's area ID.
        /// Returns a fallback "Unknown Area" if not found.
        /// </summary>
        private static TestHouseAreaModel GetArea(TileModel tile)
        {
            return TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var foundArea)
                ? foundArea
                : new TestHouseAreaModel
                {
                    Name = "Unknown Area",
                    Description = "No description available."
                };
        }

        /// <summary>
        /// Ensures that the tile description text is never empty.
        /// Uses a blank Unicode character if no text is provided.
        /// </summary>
        private static string GetTileText(TileModel tile)
        {
            return string.IsNullOrWhiteSpace(tile.TileText) ? "\u2800" : tile.TileText;
        }
        #endregion

        #region === Build Exit Info ===
        /// <summary>
        /// Builds a list of possible exits from the current tile.
        /// Shows directions and destination area names.
        /// </summary>
        private static string BuildExitInfo(TileModel tile)
        {
            if (tile.Connections == null || tile.Connections.Count == 0)
                return "None";

            return string.Join("\n", tile.Connections.Select(conn =>
            {
                // Attempt to retrieve the connected tile
                if (!TestHouseLoader.TileLookup.TryGetValue(conn, out var t))
                    return conn;

                // Determine direction (e.g., North, South, etc.)
                var dir = MapService.DetermineDirection(tile, t) ?? "Travel";

                // Get target area name
                var targetAreaName = TestHouseLoader.AreaLookup.TryGetValue(t.AreaId, out var areaT)
                    ? areaT.Name
                    : t.AreaId;

                return $"{dir} → {targetAreaName} ({t.TileId})";
            }));
        }
        #endregion

        #region === Add Lock Info ===
        /// <summary>
        /// Adds lock information to the embed if the current tile is locked.
        /// </summary>
        private static void AddLockInfo(EmbedBuilder embed, TileModel tile)
        {
            LogService.Info($"> Running AddLockInfo");

            if (!TestHouseLoader.LockLookup.TryGetValue(tile.LockId!, out var lockDef))
                return;

            LogService.Info($"LockId: {tile.LockId} KeyId: {tile.LockState!.KeyId} isLocked: {tile.LockState.Locked} KeyHole: {tile.LockState.KeyHole}");

            if (!lockDef.KeyHole && !tile.LockSwitch)
                embed.AddField("[Locked]", "The door is locked, but there is no keyhole...");
            else if (lockDef.KeyHole && !tile.LockSwitch)
                embed.AddField("[Locked]", "The door is locked.");
            else
                return;
        }
        #endregion

        #region === Logging ===
        /// <summary>
        /// Logs detailed tile information for debugging and tracing.
        /// </summary>
        private static void LogTileDebugInfo(TileModel tile, TestHouseAreaModel area, string tileTextSafe, string exitInfo)
        {
            LogService.Info(
                        $"\n[Area]\n{area.Name}\n" +
                        $"[Description]\n{area.Description}\n" +
                        $"[Location]\n{tile.TileId}\n" +
                        //$"[Layout]\n{gridVisual}" +
                        $"[Tile Text]\n{tileTextSafe}\n" +
                        $"[Exits]\n{exitInfo}\n" +
                        $"[Current Tile Id/Name]\n{tile.TileId} / {tile.TileName}\n"
            );
        }
        #endregion
    }
}
