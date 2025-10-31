using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Loaders
{
    public static class TestHouseLoader
    {
        public static Dictionary<string, TestHouseAreaModel> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        #region === Load ===
        public static List<TileModel> Load()
        {
            LogService.Info("[TestHouseLoader] Starting load of TestHouse...");

            // Load JSON data
            var (houseLayout, tileDetails) = LoadJsonData();

            // Build tiles and areas
            var allTiles = BuildTilesFromAreas(houseLayout, tileDetails);

            // Auto-connect neighbors
            BuildTileConnections(allTiles);

            LogService.Info("[TestHouseLoader] Finished building tile connections...");
            return allTiles;
        }
        #endregion

        #region === Helper methods ===
        /// <summary>
        /// Loads JSON data for the house layout and tile details.
        /// </summary>
        private static (TestHouseModel houseLayout, TestHouseTilesModel tileDetails) LoadJsonData()
        {
            var houseLayout = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");
            var tileDetails = JsonDataManager.LoadObjectFromJson<TestHouseTilesModel>("Data/Map/TestHouse/testhousetiles.json");
            return (houseLayout!, tileDetails!);
        }

        /// <summary>
        /// Builds all tiles and areas from the loaded JSON definitions.
        /// </summary>
        private static List<TileModel> BuildTilesFromAreas(TestHouseModel houseLayout, TestHouseTilesModel tileDetails)
        {
            var allTiles = new List<TileModel>();
            AreaLookup.Clear();
            TileLookup.Clear();

            foreach (var (areaId, area) in houseLayout.Areas)
            {
                area.Tiles = new List<TileModel>();

                var areaTileDetails = tileDetails.Areas.TryGetValue(area.Id, out var details)
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

                AreaLookup[area.Id] = area;
                LogService.Info($"[TestHouseLoader] Found area: {area.Name} ({areaId})\n> Added area [{area.Name}] to AreaLookup\n");
            }

            LogService.Info($"[TestHouseLoader] Loaded {AreaLookup.Count} areas and {allTiles.Count} tiles.");
            return allTiles;
        }

        /// <summary>
        /// Creates a single TileModel based on the layout and tile details.
        /// </summary>
        private static TileModel CreateTile(TestHouseAreaModel area, List<TestHouseTileDetailModel> areaTileDetails, int row, int col)
        {
            string tileType = area.Layout[row][col];
            string tilePosition = $"{row},{col}";
            string tileId = $"{area.Id}:{tilePosition}";

            var detail = areaTileDetails.FirstOrDefault(t => t.Id.Equals(tileType, StringComparison.OrdinalIgnoreCase));

            return new TileModel
            {
                TileId = tileId,
                TileName = tileType,
                AreaId = area.Id,
                TileType = tileType,
                TilePosition = tilePosition,
                TileText = detail?.Text ?? string.Empty,
                TilePOI = detail?.Pois ?? new List<string>(),
                TileItems = detail?.Items ?? new List<string>(),
                Connections = detail?.Connections ?? new List<string>(),
                TileBase = detail?.Base ?? string.Empty,
                TileOverlay = detail?.Overlay ?? string.Empty
            };
        }

        /// <summary>
        /// Automatically connects tiles to their walkable neighbors (N/S/E/W).
        /// </summary>
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

        /// <summary>
        /// Parses a tile position "row,col" into integers.
        /// </summary>
        private static (int row, int col) ParseTilePosition(string pos)
        {
            var parts = pos.Split(',');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }
    #endregion
}
