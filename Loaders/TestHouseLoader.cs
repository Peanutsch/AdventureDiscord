using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Loaders
{
    /// <summary>
    /// Static loader for the TestHouse map.
    /// Populates areas and tiles, ensuring TileId and TilePosition match savepoints.
    /// </summary>
    public static class TestHouseLoader
    {
        /// <summary>
        /// Lookup dictionary for all loaded areas, keyed by area ID.
        /// </summary>
        public static Dictionary<string, TestHouseAreaModel> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Lookup dictionary for all tiles, keyed by TileId ("areaId:row,col").
        /// </summary>
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads all TestHouse areas and tiles from JSON files.
        /// Ensures each tile has a proper TileId ("areaId:row,col"), TileType, and valid Connections.
        /// </summary>
        /// <returns>List of all loaded TileModels.</returns>
        public static List<TileModel> Load()
        {
            LogService.Info("[TestHouseLoader] Starting load of TestHouse...");

            // Load JSON map data
            var houseLayout = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");
            var tileDetails = JsonDataManager.LoadObjectFromJson<TestHouseTilesModel>("Data/Map/TestHouse/testhousetiles.json");

            var allTiles = new List<TileModel>();
            AreaLookup.Clear();
            TileLookup.Clear();

            foreach (var areaKvp in houseLayout!.Areas)
            {
                var areaId = areaKvp.Key;
                var area = areaKvp.Value;

                LogService.Info($"[TestHouseLoader] Found in testhouse.json -> areaId: {areaId}, areaName: {area.Name}");

                area.Tiles = new List<TileModel>();

                // Get detailed tile info from testhousetiles.json if available
                var areaTileDetails = tileDetails?.Areas.ContainsKey(area.Id) == true
                    ? tileDetails.Areas[area.Id]
                    : new List<TestHouseTileDetailModel>();

                LogService.Info($"[TestHouseLoader] Found in testhousetiles.json for {areaId} -> {areaTileDetails.Count} tile details.");

                // --- Build the tile grid ---
                for (int row = 0; row < area.Layout.Count; row++)
                {
                    for (int col = 0; col < area.Layout[row].Count; col++)
                    {
                        string tileType = area.Layout[row][col];   // e.g. "DOOR", "START", "Floor"
                        string tilePosition = $"{row},{col}";
                        string tileId = $"{area.Id}:{tilePosition}";

                        // Match this tile to its detailed info (if exists)
                        var detail = areaTileDetails.FirstOrDefault(t =>
                            t.Id.Equals(tileType, StringComparison.OrdinalIgnoreCase));

                        // --- Create the TileModel ---
                        var tile = new TileModel
                        {
                            TileId = tileId,
                            TileName = tileType, // bv "START", "DOOR", "Floor"
                            AreaId = area.Id,
                            TileType = tileType,
                            TilePosition = tilePosition,
                            TileText = detail?.Text ?? string.Empty,
                            Overlays = detail?.Overlays ?? new List<string>(),
                            TilePOI = detail?.Pois ?? new List<string>(),
                            TileItems = detail?.Items ?? new List<string>(),
                            Connections = detail?.Connections ?? new List<string>()
                        };

                        // Add tile to the collections
                        area.Tiles.Add(tile);
                        allTiles.Add(tile);

                        TileLookup[tile.TileId] = tile;
                        // TileLookup op basis van Name (START, DOOR, etc.)
                        if (!TileLookup.ContainsKey(tile.TileName))
                            TileLookup[tile.TileName] = tile;
                    }
                }

                // Store the area in lookup
                AreaLookup[area.Id] = area;
            }

            LogService.Info($"[TestHouseLoader] Loaded {AreaLookup.Count} areas total. Total tiles: {allTiles.Count}");
            return allTiles;
        }
    }
}