using Adventure.Models.Map;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class TestHouseLoader
    {
        #region === Dictionaries for Lookup ===
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, TestHouseAreaModel> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, TestHouseLockModel> LockLookup { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region === Load Environment ===
        public static List<TileModel> Load()
        {
            LogService.Info("[TestHouseLoader] >>> Starting load of TestHouse <<<");

            // Load JSON data
            var (houseLayout, tileDetails, lockData) = LoadTestHouseData();

            // === Load testhouse lock data: testhouselocks.json ===
            LoadLocks(lockData);

            // Build tiles
            var allTiles = BuildTilesFromAreas(houseLayout, tileDetails);

            // Apply door states
            ApplyLockStates(allTiles);

            // Auto-connect neighbors
            BuildTileConnections(allTiles);

            LogService.Info("[TestHouseLoader] >>> Finished loading TestHouse <<<");
            return allTiles;
        }
        #endregion

        #region === Load JSON ===
        /// <summary>
        /// Loads map-related data for the TestHouse environment:
        /// - The room layout (areas and tiles)
        /// - Tile-specific details
        /// - Door/lock states
        /// Applies the door lock information to the corresponding tiles based on matching LockIds.
        /// </summary>
        /// <returns>
        /// A tuple containing the house layout, tile details, and lock data.
        /// </returns>
        /// <exception cref="InvalidDataException">Thrown when any of the JSON files are invalid or missing.</exception>
        private static (TestHouseModel houseLayout, TestHouseTilesModel tileDetails, TestHouseLockCollection lockData) LoadTestHouseData()
        {
            LogService.Info("[TestHouseLoader.LoadTestHouseData] Starting TestHouse data load...");

            // === Load testhouse layout: testhouse.json ===
            var houseLayout = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");
            if (houseLayout == null)
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] Error loading testhouse.json: Data is invalid or missing.");
                throw new InvalidDataException("testhouse.json is invalid or missing.");
            }
            LogService.Info("[TestHouseLoader.LoadTestHouseData] Loaded testhouse.json successfully.");

            // === Load tile details: testhousetiles.json ===
            var tileDetails = JsonDataManager.LoadObjectFromJson<TestHouseTilesModel>("Data/Map/TestHouse/testhousetiles.json");
            if (tileDetails == null)
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] Error loading testhousetiles.json: Data is invalid or missing.");
                throw new InvalidDataException("testhousetiles.json is invalid or missing.");
            }
            LogService.Info("[TestHouseLoader.LoadTestHouseData] Loaded testhousetiles.json successfully.");

            // === Load door lock configuration: testhouselocks.json ===
            var lockData = JsonDataManager.LoadObjectFromJson<TestHouseLockCollection>("Data/Map/TestHouse/testhouselocks.json");
            if (lockData == null)
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] Error loading testhouselocks.json: Data is invalid or missing.");
                throw new InvalidDataException("testhouselocks.json is invalid or missing.");
            }
            LogService.Info($"[TestHouseLoader.LoadTestHouseData] Loaded testhouselocks.json successfully with {lockData.LockedDoors.Count} entries.");

            // === Validate door data structure ===
            if (lockData.LockedDoors == null || lockData.LockedDoors.Count == 0)
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] No door locks found — proceeding with all doors unlocked.");
            }
            else if (lockData.LockedDoors.Any(d => string.IsNullOrEmpty(d.Key) || d.Value == null))
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] Invalid door data: Missing or incorrect lock data.");
                throw new InvalidDataException("Invalid door data in testhouselocks.json.");
            }

            LogService.Info($"[TestHouseLoader.LoadTestHouseData] Loaded tile details for {tileDetails.Areas.Count} areas.");

            LogService.Info($"[TestHouseLoader.LoadTestHouseData] TestHouse data load completed successfully.\n");

            return (houseLayout!, tileDetails!, lockData!);
        }
        #endregion

        #region === Tile Creation ===
        /// <summary>
        /// Builds all tiles for the TestHouse map based on the area layouts and tile detail definitions. 
        /// Also initializes the TileLookup and AreaLookup dictionaries.
        /// </summary>
        /// <param name="houseLayout">
        /// The structural layout of the TestHouse, including all defined areas and their grid-based layouts.
        /// </param>
        /// <param name="tileDetails">
        /// Detailed tile metadata such as descriptions, items, connections, and lock information.
        /// </param>
        /// <returns>
        /// A flat list containing all generated tiles across all areas.
        /// </returns>
        private static List<TileModel> BuildTilesFromAreas(TestHouseModel houseLayout, TestHouseTilesModel tileDetails)
        {
            var allTiles = new List<TileModel>();

            // Reset lookup tables before rebuilding tiles
            TileLookup.Clear();
            AreaLookup.Clear();

            foreach (var (areaId, area) in houseLayout.Areas)
            {
                // Initialize the tile list for the current area
                area.Tiles = new List<TileModel>();

                // Retrieve tile details for this area, if available
                var areaTileDetails = tileDetails.Areas.TryGetValue(areaId, out var details)
                    ? details
                    : new List<TestHouseTileDetailModel>();

                // Iterate through the area's grid layout
                for (int row = 0; row < area.Layout.Count; row++)
                {
                    for (int col = 0; col < area.Layout[row].Count; col++)
                    {
                        // Create and register a tile for this grid position
                        var tile = CreateTile(area, areaTileDetails, row, col);

                        area.Tiles.Add(tile);
                        allTiles.Add(tile);

                        // Register tile in the global lookup using its unique TileId
                        TileLookup[tile.TileId] = tile;
                    }
                }

                // Register the area in the area lookup table
                AreaLookup[areaId] = area;
            }

            LogService.Info($"[TestHouseLoader.BuildTilesFromAreas] Loaded {AreaLookup.Count} areas and {allTiles.Count} tiles.\n");

            return allTiles;
        }

        /// <summary>
        /// Creates a single tile instance based on the area's layout grid and
        /// the optional tile detail metadata for that area.
        /// </summary>
        /// <param name="area">
        /// The area to which this tile belongs.
        /// </param>
        /// <param name="areaTileDetails">
        /// A collection of tile detail definitions for the current area.
        /// </param>
        /// <param name="row">
        /// The row index within the area's grid layout.
        /// </param>
        /// <param name="col">
        /// The column index within the area's grid layout.
        /// </param>
        /// <returns>
        /// A fully initialized <see cref="TileModel"/> instance.
        /// </returns>
        private static TileModel CreateTile(TestHouseAreaModel area, List<TestHouseTileDetailModel> areaTileDetails, int row, int col)
        {
            // Determine tile type and position from the layout grid
            string tileType = area.Layout[row][col];
            string tilePosition = $"{row},{col}";
            string tileId = $"{area.Id}:{tilePosition}";

            // Attempt to find matching tile metadata for this tile type
            var areaDetail = areaTileDetails.FirstOrDefault(t =>
                t.Id.Equals(tileType, StringComparison.OrdinalIgnoreCase));

            // Create and populate the tile model
            var tile = new TileModel
            {
                TileId = tileId,
                TileName = tileType,
                TilePosition = tilePosition,
                TileType = tileType,
                AreaId = area.Id,

                // Optional metadata from tile detail definitions
                TileText = areaDetail?.Text ?? string.Empty,
                TilePOI = areaDetail?.Pois ?? new List<string>(),
                TileItems = areaDetail?.Items ?? new List<string>(),
                Connections = areaDetail?.Connections ?? new List<string>(),
                TileBase = areaDetail?.Base ?? string.Empty,
                TileOverlay = areaDetail?.Overlay ?? string.Empty,

                // Lock-related metadata (resolved later by ApplyLockStates)
                LockId = areaDetail?.TileLockId,
                LockSwitch = areaDetail?.TileLockSwitch ?? false
            };

            if (tile.LockSwitch && !string.IsNullOrEmpty(tile.LockId))
            {
                LogService.Info($"[TestHouseLoader.CreateTile] Created tile {tile.TileId} with LockSwitch → Connected with LockId: '{tile.LockId}'");
            }

            return tile;
        }
        #endregion

        #region === Tile Connections ===
        /// <summary>
        /// Builds bidirectional connections between adjacent tiles based on their grid positions. 
        /// Only tiles with connectable types are linked.
        /// </summary>
        /// <param name="allTiles">
        /// A collection of all tiles that were created for the map.
        /// </param>
        private static void BuildTileConnections(List<TileModel> allTiles)
        {
            // Tile types to be ignored as possible connections
            var nonConnectable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    "Wall",
                                    "Water",
                                    "BLOCKt"
                                };

            foreach (var tile in allTiles)
            {
                // Skip tiles that are not allowed to connect to neighbors
                if (nonConnectable.Contains(tile.TileType))
                    continue;

                // Parse the tile's grid position (row, column)
                var (row, col) = ParseTilePosition(tile.TilePosition);

                // Define the neighbor positions
                var directions = new (string direction, int row, int col)[]
                                {
                                    ("Up",    row - 1, col),
                                    ("Down",  row + 1, col),
                                    ("Left",  row,     col - 1),
                                    ("Right", row,     col + 1)
                                };

                foreach (var (_, r, c) in directions)
                {
                    // Build the lookup key to identify neighboring tiles
                    string targetKey = $"{tile.AreaId}:{r},{c}";

                    // Check if a valid neighboring tile exists and is connectable
                    if (TileLookup.TryGetValue(targetKey, out var neighbor) &&
                        !nonConnectable.Contains(neighbor.TileType))
                    {
                        // Prevent duplicate connections
                        if (!tile.Connections.Contains(neighbor.TileId))
                        {
                            tile.Connections.Add(neighbor.TileId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parses a tile position string into its numeric grid coordinates.
        /// Format: "row,column" (e.g. "4,12").
        /// </summary>
        /// <param name="pos">
        /// The tile position string to parse.
        /// </param>
        /// <returns>
        /// A tuple containing the row and column values.
        /// </returns>
        private static (int row, int col) ParseTilePosition(string pos)
        {
            // Split the position string by comma
            var parts = pos.Split(',');

            // Convert both parts into integers
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
        #endregion

        #region === Lock Handling ===
        /// <summary>
        /// Loads all lock definitions from the TestHouse lock JSON file into memory.
        /// This method initializes the <see cref="LockLookup"/> dictionary.
        /// </summary>
        public static void LoadLocks(TestHouseLockCollection lockData)
        {
            // Load lock data from JSON
            //var lockData = JsonDataManager.LoadObjectFromJson<TestHouseLockCollection>("Data/Map/Testhouse/testhouselocks.json");

            // Ensure a clean state before loading
            LockLookup.Clear();

            // Register each lock using the dictionary key as its unique identifier
            foreach (var (lockId, lockDef) in lockData!.LockedDoors)
            {
                // Store the lock definition under its lockId
                LockLookup[lockId] = lockDef;

                LogService.Info($"[TestHouseLoader.LoadLocks] Loading {lockId}. KeyId: {lockDef.KeyId} LockType: {lockDef.LockType} isLocked: {lockDef.Locked}");
            }

            LogService.Info($"[TestHouseLoader.LoadLocks] Loaded {LockLookup.Count} locks\n");
        }

        /// <summary>
        /// Applies loaded lock definitions to the corresponding tiles.
        /// Each tile that declares a LockId will receive a reference to the matching
        /// lock instance from <see cref="LockLookup"/>.
        /// </summary>
        /// <param name="allTiles">
        /// The complete list of tiles that were generated for the TestHouse map.
        /// </param>
        public static void ApplyLockStates(List<TileModel> allTiles)
        {
            foreach (var tile in allTiles)
            {
                // Skip tiles that do not reference a lock
                if (string.IsNullOrEmpty(tile.LockId))
                    continue;

                // Attempt to resolve the lock definition by lockId
                if (LockLookup.TryGetValue(tile.LockId, out var lockDef))
                {
                    // Assign the lock state to the tile (shared instance)
                    tile.LockState = lockDef;

                    LogService.Info($"[TestHouseLoader.ApplyLockStates] Applied lock '{tile.LockId}' to tile {tile.TileId}");
                }
            }
        }
        #endregion
    }
}
