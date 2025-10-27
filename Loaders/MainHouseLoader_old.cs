using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Loaders
{
    /// <summary>
    /// Static loader for the main house map.
    /// Handles loading rooms and tiles from JSON, building a lookup dictionary,
    /// and storing descriptions for each room.
    /// </summary>
    /*
    public static class MainHouseLoader_old
    {
        // --- Flattened list of all tiles in the main house ---
        public static List<TileModel> AllTiles { get; private set; } = new();

        // --- Dictionary for quick tile lookup by "RoomName:TileId" ---
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // --- Dictionary of the areas, each containing a list of its tiles ---
        public static Dictionary<string, List<TileModel>> AreaTiles { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // --- Lookup of all loaded areas, each containing its tiles, name, and description ---
        public static Dictionary<string, MainHouseAreaModel_old> AreaLookup { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads all rooms and tiles from the mainhouse JSON file.
        /// Builds the tile lookup and room description dictionaries.
        /// </summary>
        /// <returns>List of all tiles loaded, or null if loading fails.</returns>
        public static List<TileModel>? Load()
        {
            try
            {
                // --- Load mainhouse.json into MainHouseModel_old ---
                var mainhouse = JsonDataManager.LoadObjectFromJson<MainHouseModel_old>("Data/Map/MainHouse/mainhouse.json");

                // --- Check for null or empty rooms ---
                if (mainhouse == null || mainhouse.Area == null || mainhouse.Area.Count == 0)
                {
                    LogService.Error("[MainHouseLoader_old] > No area found");
                    return null;
                }

                // --- Clear previous data ---
                AllTiles.Clear();
                TileLookup.Clear();
                AreaTiles.Clear();

                // --- Process all top-level rooms ---
                foreach (var area in mainhouse.Area)
                    ProcessArea(area.Key, area.Value);

                // --- Logging summary ---
                LogService.Info($"[MainHouseLoader_old] > Loaded {AreaLookup.Count} areas");
                LogService.Info($"[MainHouseLoader_old] > Loaded {AllTiles.Count} tiles\n");

                return AllTiles;
            }
            catch (Exception ex)
            {
                // --- Log any exception during loading ---
                LogService.Error($"[MainHouseLoader_old] > Error loading mainhouse.json: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes a room recursively: stores tiles, builds lookup, and stores description.
        /// Handles subrooms as well.
        /// </summary>
        /// <param name="areaName">The name of the room (parent prefix included for subrooms).</param>
        /// <param name="room">The room model from JSON.</param>
        static void ProcessArea(string areaName, MainHouseAreaModel_old area)
        {
            LogService.Info($"[MainHouseLoader_old.ProcessArea] Processing areaName: {areaName}");

            AreaLookup[area.Id] = area;
            LogService.Info($"[MainHouseLoader_old.ProcessArea] [{area.Id}] added to dict AreaLookup");

            // --- Store tiles for this room ---
            if (area.Tiles != null)
            {
                AreaTiles[areaName] = area.Tiles;      // Save tiles in Areas dictionary
                AllTiles.AddRange(area.Tiles);     // Add to flattened list

                LogService.Info($"[MainHouseLoader_old.ProcessArea] Loading {areaName}, {area.Tiles.Count} tiles...");

                // --- Build TileLookup for quick access ---
                foreach (var tile in area.Tiles)
                {
                    tile.AreaId = area.Id;
                    if (!string.IsNullOrWhiteSpace(tile.TilePosition))
                    {
                        string key = $"{area.Id}:{tile.TileId}";
                        if (!TileLookup.ContainsKey(key))
                            TileLookup[key] = tile; // Add tile to lookup
                    }
                }
            }
        }
    }
    */
}
