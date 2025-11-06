using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Adventure.Loaders
{
    public static class TestHouseLoader
    {
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, TestHouseAreaModel> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        #region === Load Environment ===
        public static List<TileModel> Load()
        {
            LogService.Info("[TestHouseLoader] Starting load of TestHouse...");

            // Load JSON data
            var (houseLayout, tileDetails, lockData) = LoadTestHouseData();

            // Build tiles
            var allTiles = BuildTilesFromAreas(houseLayout, tileDetails);

            // Apply door states
            ApplyLockStates(allTiles, lockData);

            // Auto-connect neighbors
            BuildTileConnections(allTiles);

            LogService.Info("[TestHouseLoader] Finished loading TestHouse.");
            return allTiles;
        }
        #endregion

        #region === Load JSON ===
        /// <summary>
        /// Loads all map-related data for the TestHouse environment, including:
        /// - The room layout (areas and tiles)
        /// - Tile-specific details
        /// - Door/lock states
        /// After loading, the method automatically applies the door lock information
        /// to the corresponding tiles based on matching LockIds.
        /// </summary>
        /// <returns>
        /// A tuple containing the house layout, tile details, and lock collection.
        /// </returns>
        /// <exception cref="InvalidDataException">Thrown when any of the JSON files are invalid or missing.</exception>
        private static (TestHouseModel houseLayout, TestHouseTilesModel tileDetails, TestHouseLockCollection doorData) LoadTestHouseData()
        {
            LogService.Info("[TestHouseLoader.LoadTestHouseData] Starting TestHouse data load...");

            // === Load testhouse layout testhouse.json ===
            var houseLayout = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");
            if (houseLayout == null)
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] Error loading testhouse.json: Data is invalid or missing.");
                throw new InvalidDataException("testhouse.json is invalid or missing.");
            }
            LogService.Info("[TestHouseLoader.LoadTestHouseData] Loaded testhouse.json successfully.");

            // === Load tile details testhousetiles.json ===
            var tileDetails = JsonDataManager.LoadObjectFromJson<TestHouseTilesModel>("Data/Map/TestHouse/testhousetiles.json");
            if (tileDetails == null)
            {
                LogService.Error("[TestHouseLoader.LoadTestHouseData] Error loading testhousetiles.json: Data is invalid or missing.");
                throw new InvalidDataException("testhousetiles.json is invalid or missing.");
            }
            LogService.Info("[TestHouseLoader.LoadTestHouseData] Loaded testhousetiles.json successfully.");

            // === Load door lock configuration testhouselocks.json ===
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

            // === Apply lock states to all tiles ===
            LogService.Info($"[TestHouseLoader.LoadTestHouseData] Loaded tile details for {tileDetails.Areas.Count} areas.");

            LogService.Info("[TestHouseLoader.LoadTestHouseData] TestHouse data load completed successfully.");
            return (houseLayout!, tileDetails!, lockData!);
        }
        #endregion

        #region === Tile Creation ===
        private static List<TileModel> BuildTilesFromAreas(TestHouseModel houseLayout, TestHouseTilesModel tileDetails)
        {
            var allTiles = new List<TileModel>();
            TileLookup.Clear();
            AreaLookup.Clear();

            foreach (var (areaId, area) in houseLayout.Areas)
            {
                area.Tiles = new List<TileModel>();

                var areaTileDetails = tileDetails.Areas.TryGetValue(areaId, out var details)
                    ? details
                    : new List<TestHouseTileDetailModel>();

                for (int row = 0; row < area.Layout.Count; row++)
                {
                    for (int col = 0; col < area.Layout[row].Count; col++)
                    {
                        var tile = CreateTile(area, areaTileDetails, row, col);
                        area.Tiles.Add(tile);
                        allTiles.Add(tile);
                        TileLookup[tile.TileId] = tile;
                    }
                }

                // --- Add to lookup list AreaLookup
                AreaLookup[areaId] = area;
            }

            LogService.Info($"[TestHouseLoader] Loaded {AreaLookup.Count} areas and {allTiles.Count} tiles.");
            return allTiles;
        }

        private static TileModel CreateTile(TestHouseAreaModel area, List<TestHouseTileDetailModel> areaTileDetails, int row, int col)
        {
            string tileType = area.Layout[row][col];
            string tilePosition = $"{row},{col}";
            string tileId = $"{area.Id}:{tilePosition}";

            // Find matching details for this tile type
            var areaDetail = areaTileDetails
                .FirstOrDefault(t => t.Id.Equals(tileType, StringComparison.OrdinalIgnoreCase));

            // Create tile
            var tile = new TileModel
            {
                TileId = tileId,
                TileName = tileType,
                TilePosition = tilePosition,
                TileType = tileType,
                AreaId = area.Id,
                TileText = areaDetail?.Text ?? string.Empty,
                TilePOI = areaDetail?.Pois ?? new List<string>(),
                TileItems = areaDetail?.Items ?? new List<string>(),
                Connections = areaDetail?.Connections ?? new List<string>(),
                TileBase = areaDetail?.Base ?? string.Empty,
                TileOverlay = areaDetail?.Overlay ?? string.Empty,
                LockId = areaDetail?.TileLockId,
                LockSwitch = areaDetail?.TileLockSwitch ?? false
            };

            // --- Debug logging for tile lock info ---
            if (tile.LockSwitch && !string.IsNullOrEmpty(tile.LockId))
            {
                LogService.Info($"[CreateTile] Created tile {tile.TileId} → LockSwitch={tile.LockSwitch}, LockId='{tile.LockId}'");
            }

            return tile;
        }
        #endregion

        #region === Lock Handling ===
        public static Dictionary<string, TestHouseLockModel> LockLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        private static void ApplyLockStates(List<TileModel> allTiles, TestHouseLockCollection doorData)
        {
            foreach (var tile in allTiles)
            {
                // Skip tiles without a LockId
                if (string.IsNullOrEmpty(tile.LockId))
                    continue;

                // Apply matching lock state if available
                if (doorData.LockedDoors.TryGetValue(tile.LockId!, out var doorState))
                {
                    tile.LockState = new TestHouseLockModel
                    {
                        LockType = doorState.LockType,
                        Locked = doorState.Locked
                    };

                    LockLookup[tile.LockId!] = tile.LockState;
                    LogService.Info($"[TestHouseLoader.ApplyDoorStates] Applied lock '{tile.LockId}' to tile {tile.TileId} → {doorState.LockType}/{doorState.Locked}");
                }
            }
        }
        #endregion

        #region === Tile Connections ===
        private static void BuildTileConnections(List<TileModel> allTiles)
        {
            var nonConnectable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Wall", "Water", "BLOCKt"
            };

            foreach (var tile in allTiles)
            {
                if (nonConnectable.Contains(tile.TileType))
                    continue;

                var (row, col) = ParseTilePosition(tile.TilePosition);

                var directions = new (string dir, int r, int c)[]
                {
                    ("Up", row - 1, col),
                    ("Down", row + 1, col),
                    ("Left", row, col - 1),
                    ("Right", row, col + 1)
                };

                foreach (var (_, r, c) in directions)
                {
                    string targetKey = $"{tile.AreaId}:{r},{c}";
                    if (TileLookup.TryGetValue(targetKey, out var neighbor) &&
                        !nonConnectable.Contains(neighbor.TileType))
                    {
                        if (!tile.Connections.Contains(neighbor.TileId))
                            tile.Connections.Add(neighbor.TileId);
                    }
                }
            }
        }

        private static (int row, int col) ParseTilePosition(string pos)
        {
            var parts = pos.Split(',');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
        #endregion
    }
}
