using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using Discord;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Builds Discord embeds for map display, tile information, and navigation.
    /// 
    /// This static class is responsible for:
    /// - Creating visually formatted embeds showing the player's current location
    /// - Rendering ASCII/emoji grid layouts of the map area
    /// - Displaying tile descriptions and interactive elements
    /// - Managing lock and door state information
    /// - Calculating and displaying available exits
    /// 
    /// Embeds created by this class are sent to Discord as rich formatted messages
    /// with colors, fields, and attached navigation components.
    /// 
    /// <remarks>
    /// Usage Workflow:
    /// 1. Player moves to a tile
    /// 2. EmbedWalk() is called with the new tile
    /// 3. Embed is built with area info, grid, and lock status
    /// 4. Embed is sent to Discord with navigation buttons
    /// 
    /// Embed Structure:
    /// ┌─────────────────────────────┐
    /// │ [Area Name] - Area Desc      │
    /// │ [Grid Visual with Player]    │
    /// │ Tile Text Description        │
    /// │ [Lock Info] (if applicable)  │
    /// └─────────────────────────────┘
    /// </remarks>
    /// </summary>
    public static class EmbedBuildersMap
    {
        #region === Embed ===

        /// <summary>
        /// Builds a complete Discord embed displaying the player's current map location.
        /// 
        /// The embed contains:
        /// 1. Area name and description
        /// 2. ASCII/emoji grid showing the current area layout with player position
        /// 3. Tile-specific text description
        /// 4. Lock information (if the tile is locked)
        /// 
        /// This is the primary method for generating map display embeds sent to players.
        /// </summary>
        /// <param name="tile">The TileModel representing the player's current position.</param>
        /// <returns>
        /// A fully constructed EmbedBuilder ready to be sent to Discord via FollowupAsync().
        /// The embed has a blue color scheme.
        /// </returns>
        /// <remarks>
        /// Process:
        /// 1. Fetch area data from tile's AreaId
        /// 2. Render grid layout with player marker
        /// 3. Get tile description (with fallback to blank if missing)
        /// 4. Check locks and apply toggle logic if tile is a switch
        /// 5. Build embed fields with all information
        /// 6. Add lock information if tile is locked
        /// 7. Log all data for debugging
        /// 
        /// Example Output:
        /// [Tavern - The Rusty Lion]
        /// 🌳🌳🌳  (grid display)
        /// ⬜🧍⬛
        /// 🌳🟫🌳
        /// "A cozy tavern filled with adventurers..."
        /// 
        /// [Locked] The door is locked with a BRONZE lock.
        /// </remarks>
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersMap.EmbedWalk] Building embed...");

            // Step 1: Fetch basic area and tile data
            var area = GetArea(tile);
            string gridVisual = TileUI.RenderTileGrid(tile);
            string tileTextSafe = GetTileText(tile);
            string exitInfo = BuildExitInfo(tile);

            // Step 2: Handle locks (toggle if tile acts as a switch)
            TestHouseLockService.ToggleLockBySwitch(tile, TestHouseLoader.LockLookup);

            // Step 3: Truncate area name if too long to prevent embed field overflow
            string safeAreaName = area.Name.Length > 250
                ? area.Name.Substring(0, 250) + "..."
                : area.Name;

            // Step 4: Build the main embed structure with title, description, and grid
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .AddField($"[{safeAreaName}]", area.Description)
                .AddField($"{gridVisual}\n", $"{tileTextSafe}");

            // Step 5: Add extra info about lock state if tile is locked
            if (tile.LockState!.Locked || tile.LockSwitch)
                AddLockInfo(embed, tile);

            // Step 6: Log all collected tile information for debugging and tracing
            LogTileDebugInfo(tile, area, tileTextSafe, exitInfo);

            return embed;
        }

        #endregion

        #region === Get Area Data and Tile Text ===

        /// <summary>
        /// Retrieves the TestHouseAreaModel associated with a tile's AreaId.
        /// 
        /// Looks up the area in TestHouseLoader.AreaLookup and returns the full area information
        /// including name, description, and tile layout. If the area cannot be found, returns
        /// a fallback "Unknown Area" to prevent null reference exceptions.
        /// </summary>
        /// <param name="tile">The TileModel whose area should be retrieved.</param>
        /// <returns>
        /// The TestHouseAreaModel if found, otherwise a default area with 
        /// name "Unknown Area" and description "No description available."
        /// </returns>
        /// <remarks>
        /// Fallback Behavior:
        /// If tile.AreaId doesn't exist in the map loader, a generic area is created
        /// to gracefully handle missing data without crashing.
        /// 
        /// This prevents embeds from failing to render if area data is inconsistent.
        /// </remarks>
        private static TestHouseAreaModel GetArea(TileModel tile)
        {
            // Attempt to find the area in the loader's area dictionary
            return TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out var foundArea)
                ? foundArea
                // Fallback to generic area if not found
                : new TestHouseAreaModel
                {
                    Name = "Unknown Area",
                    Description = "No description available."
                };
        }

        /// <summary>
        /// Sanitizes the tile description text to ensure it's never empty.
        /// 
        /// Returns the tile's TileText if present and non-empty. Otherwise returns
        /// a Unicode blank space character (U+2800) to ensure the embed field renders
        /// without appearing completely empty.
        /// </summary>
        /// <param name="tile">The TileModel from which to extract text.</param>
        /// <returns>
        /// Either the tile's original text, or a blank Unicode character if text is missing.
        /// Never returns an empty string.
        /// </returns>
        /// <remarks>
        /// Why Blank Space?
        /// Discord embeds with completely empty field values can appear broken or incomplete.
        /// Using \u2800 (braille blank) provides visual spacing while indicating 
        /// no text was explicitly provided.
        /// 
        /// This is a defensive technique to handle poorly initialized tile data gracefully.
        /// </remarks>
        private static string GetTileText(TileModel tile)
        {
            // Return original text if present, otherwise use blank Unicode character
            return string.IsNullOrWhiteSpace(tile.TileText) ? "\u2800" : tile.TileText;
        }

        #endregion

        #region === Build Exit Info ===

        /// <summary>
        /// Builds a formatted list of all available exits from the current tile.
        /// 
        /// For each connection, retrieves the connected tile, determines the direction
        /// (North, South, East, West, etc.), and gets the target area name.
        /// The result is a human-readable navigation summary.
        /// </summary>
        /// <param name="tile">The current TileModel whose exits should be determined.</param>
        /// <returns>
        /// A newline-separated string listing all available exits with directions and destinations.
        /// Format: "North → Tavern (area_tavern:5,3)\nEast → Forest (area_forest:2,1)"
        /// Returns "None" if the tile has no connections.
        /// </returns>
        /// <remarks>
        /// Exit Format:
        /// {Direction} → {Area Name} ({TileId})
        /// 
        /// Example:
        /// North → Castle (area_castle:0,5)
        /// South → Market Square (area_market:10,8)
        /// West → Tavern (area_tavern:5,3)
        /// 
        /// Direction Detection:
        /// Uses MapService.DetermineDirection() to calculate compass direction
        /// between current and target tile coordinates. Falls back to "Travel" if
        /// direction cannot be determined.
        /// </remarks>
        private static string BuildExitInfo(TileModel tile)
        {
            // Return "None" if tile has no exits
            if (tile.Connections == null || tile.Connections.Count == 0)
                return "None";

            // Build exit descriptions by joining all connections
            return string.Join("\n", tile.Connections.Select(conn =>
            {
                // Attempt to retrieve the connected tile
                if (!TestHouseLoader.TileLookup.TryGetValue(conn, out var t))
                    return conn;

                // Determine compass direction (e.g., North, South, etc.)
                var dir = MapService.DetermineDirection(tile, t) ?? "Travel";

                // Get target area name for display
                var targetAreaName = TestHouseLoader.AreaLookup.TryGetValue(t.AreaId, out var areaT)
                    ? areaT.Name
                    : t.AreaId;

                // Format as: "North → Tavern (area_tavern:5,3)"
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
                embed.AddField("[Locked]", $"The door is locked with an **{tile.LockState.LockType.ToUpper()}** lock.");
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
                        $"[Tile Text]\n{tileTextSafe}\n" +
                        $"[Exits]\n{exitInfo}\n" +
                        $"[Current Tile Id/Name]\n{tile.TileId} / {tile.TileName}\n"
            );
        }

        #endregion
    }
}
