using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Loaders
{
    /// <summary>
    /// Loader for the main house map.
    /// Handles loading all rooms and tiles from JSON, 
    /// building a tile lookup dictionary, and organizing tiles per room.
    /// </summary>
    public static class MainHouseLoader
    {
        /// <summary>
        /// All tiles in the main house, flattened into a single list.
        /// </summary>
        public static List<TileModel> AllTiles { get; private set; } = new();

        /// <summary>
        /// Lookup dictionary for quick access to tiles using "RoomName:TilePosition" as the key.
        /// </summary>
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new();

        /// <summary>
        /// Dictionary of rooms, each containing its list of tiles.
        /// Useful for building embeds or UI elements.
        /// </summary>
        public static Dictionary<string, List<TileModel>> Rooms { get; private set; } = new();

        /// <summary>
        /// Loads all tiles and rooms from the mainhouse JSON file.
        /// </summary>
        /// <returns>List of all tiles loaded, or null if loading failed.</returns>
        public static List<TileModel>? Load()
        {
            try
            {
                // --- Load main house JSON into MainHouseModel ---
                var mainhouse = JsonDataManager.LoadObjectFromJson<MainHouseModel>("Data/Map/MainHouse/mainhouse.json");

                if (mainhouse == null)
                {
                    LogService.Error("[MainHouseLoader] > Failed to load mainhouse.json");
                    return null;
                }

                if (mainhouse.Rooms == null || mainhouse.Rooms.Count == 0)
                {
                    LogService.Error("[MainHouseLoader] > No rooms found in mainhouse.json");
                    return new List<TileModel>();
                }

                var allRoomsMainHouse = new List<TileModel>();
                TileLookup = new Dictionary<string, TileModel>();

                // --- Iterate over each room in JSON ---
                foreach (var room in mainhouse.Rooms)
                {
                    string roomName = room.Key;
                    var tiles = room.Value;

                    LogService.Info($"[MainHouseLoader] > Adding {roomName}: {tiles.Count} tiles");

                    // Save room tiles in the Rooms dictionary
                    Rooms[roomName] = tiles;

                    // Add all tiles to the flattened list
                    allRoomsMainHouse.AddRange(tiles);

                    // Build TileLookup dictionary with "RoomName:TilePosition" keys
                    foreach (var tile in tiles)
                    {
                        if (string.IsNullOrWhiteSpace(tile.TilePosition))
                            continue; // Skip tiles without a position

                        string key = $"{roomName}:{tile.TileId}";

                        if (!TileLookup.ContainsKey(key))
                        {
                            LogService.Info($"[MainHouseLoader] Key added: {key}");
                            TileLookup[key] = tile;
                        }
                        else
                            LogService.Error($"[MainHouseLoader] > Duplicate key skipped: {key}");
                    }
                }

                AllTiles = allRoomsMainHouse;

                LogService.Info($"[MainHouseLoader] > Loaded total of {allRoomsMainHouse.Count} tiles from mainhouse.json\n");
                LogService.Info($"[MainHouseLoader] > TileLookup contains {TileLookup.Count} unique tiles");

                return allRoomsMainHouse;
            }
            catch (Exception ex)
            {
                LogService.Error($"[MainHouseLoader] > Error loading mainhouse.json:\n{ex.Message}");
                return null;
            }
        }
    }
}