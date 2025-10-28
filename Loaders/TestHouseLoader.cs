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

        public static List<TileModel> Load()
        {
            LogService.Info("[TestHouseLoader] Starting load of TestHouse...");

            var houseLayout = JsonDataManager.LoadObjectFromJson<TestHouseModel>("Data/Map/TestHouse/testhouse.json");
            var tileDetails = JsonDataManager.LoadObjectFromJson<TestHouseTilesModel>("Data/Map/TestHouse/testhousetiles.json");

            var allTiles = new List<TileModel>();
            AreaLookup.Clear();
            TileLookup.Clear();

            foreach (var areaKvp in houseLayout!.Areas)
            {
                var areaId = areaKvp.Key;
                var area = areaKvp.Value;
                LogService.Info($"[TestHouseLoader] Found area: {areaId} ({area.Name})");

                area.Tiles = new List<TileModel>();

                var areaTileDetails = tileDetails?.Areas.ContainsKey(area.Id) == true
                    ? tileDetails.Areas[area.Id]
                    : new List<TestHouseTileDetailModel>();

                for (int row = 0; row < area.Layout.Count; row++)
                {
                    for (int col = 0; col < area.Layout[row].Count; col++)
                    {
                        string tileType = area.Layout[row][col]; // bv: "DOOR", "Floor", "Wall"
                        string tilePosition = $"{row},{col}";
                        string tileId = $"{area.Id}:{tilePosition}";

                        var detail = areaTileDetails.FirstOrDefault(t =>
                            t.Id.Equals(tileType, StringComparison.OrdinalIgnoreCase));

                        var tile = new TileModel
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

                        // Voeg tile toe aan Area en algemene lijst
                        area.Tiles.Add(tile);
                        allTiles.Add(tile);

                        // Voeg tile correct toe aan TileLookup
                        TileLookup[tile.TileId] = tile;
                        LogService.Info($"Added {tile.TileId} to List TileLookup");
                    }
                }

                AreaLookup[area.Id] = area;
                LogService.Info($"Added area [{area.Name}] to List AreaLookup");
            }

            LogService.Info($"[TestHouseLoader] Loaded {AreaLookup.Count} areas and {allTiles.Count} tiles.");

            // --- Auto-connect buurt-tiles voor Floor (N/S/E/W) ---
            foreach (var tile in allTiles)
            {
                if (tile.TileType == "Wall" || tile.TileType == "Water") continue;

                var parts = tile.TilePosition.Split(',');
                int row = int.Parse(parts[0]);
                int col = int.Parse(parts[1]);

                var directions = new Dictionary<string, (int r, int c)>
                {
                    { "North", (row - 1, col) }, // omhoog
                    { "South", (row + 1, col) }, // omlaag
                    { "West",  (row, col - 1) }, // links
                    { "East",  (row, col + 1) }  // rechts
                };

                foreach (var dir in directions)
                {
                    string targetKey = $"{tile.AreaId}:{dir.Value.r},{dir.Value.c}";
                    if (TileLookup.TryGetValue(targetKey, out var neighbor))
                    {
                        if (neighbor.TileType != "Wall" && neighbor.TileType != "Water")
                        {
                            if (!tile.Connections.Contains(neighbor.TileId))
                                tile.Connections.Add(neighbor.TileId);
                        }
                    }
                }
            }

            LogService.Info("[TestHouseLoader] Finished building tile connections.");
            return allTiles;
        }
    }
}
