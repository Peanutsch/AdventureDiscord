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
    public static class MainHouseLoader
    {
        /// <summary>
        /// Flattened list of all tiles in the main house.
        /// </summary>
        public static List<TileModel> AllTiles { get; private set; } = new();

        /// <summary>
        /// Dictionary for quick tile lookup by "RoomName:TileId".
        /// </summary>
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new();

        /// <summary>
        /// Dictionary of rooms, each containing a list of its tiles.
        /// </summary>
        public static Dictionary<string, List<TileModel>> Area { get; private set; } = new();

        /// <summary>
        /// Dictionary of room descriptions for display or embeds.
        /// </summary>
        public static Dictionary<string, string> AreaDescriptions { get; private set; } = new();

        public static Dictionary<string, MainHouseAreaModel> AreaModels { get; private set; } = new();


        /// <summary>
        /// Loads all rooms and tiles from the mainhouse JSON file.
        /// Builds the tile lookup and room description dictionaries.
        /// </summary>
        /// <returns>List of all tiles loaded, or null if loading fails.</returns>
        public static List<TileModel>? Load()
        {
            try
            {
                // --- Load mainhouse.json into MainHouseModel ---
                var mainhouse = JsonDataManager.LoadObjectFromJson<MainHouseModel>("Data/Map/MainHouse/mainhouse.json");

                // --- Check for null or empty rooms ---
                if (mainhouse == null || mainhouse.Area == null || mainhouse.Area.Count == 0)
                {
                    LogService.Error("[MainHouseLoader] > No area found");
                    return null;
                }

                // --- Clear previous data ---
                AllTiles.Clear();
                TileLookup.Clear();
                Area.Clear();
                AreaDescriptions.Clear();

                // --- Process all top-level rooms ---
                foreach (var area in mainhouse.Area)
                    ProcessArea(area.Key, area.Value);

                // --- Logging summary ---
                LogService.Info($"[MainHouseLoader] > Loaded {AllTiles.Count} tiles");
                LogService.Info($"[MainHouseLoader] > {AreaDescriptions.Count} rooms loaded");

                return AllTiles;
            }
            catch (Exception ex)
            {
                // --- Log any exception during loading ---
                LogService.Error($"[MainHouseLoader] > Error loading mainhouse.json: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes a room recursively: stores tiles, builds lookup, and stores description.
        /// Handles subrooms as well.
        /// </summary>
        /// <param name="areaName">The name of the room (parent prefix included for subrooms).</param>
        /// <param name="room">The room model from JSON.</param>
        static void ProcessArea(string areaName, MainHouseAreaModel area)
        {
            // --- Store tiles for this room ---
            if (area.Tiles != null)
            {
                Area[areaName] = area.Tiles;      // Save tiles in Rooms dictionary
                AllTiles.AddRange(area.Tiles);     // Add to flattened list

                LogService.Info($"[MainHouseLoader.ProcessArea] Loading {areaName}, {area.Tiles.Count} tiles...");

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

            // --- Store room description ---
            AreaDescriptions[areaName] = area.Description;
            LogService.Info($"[MainHouseLoader.ProcessArea] Storing description {areaName}");

            // --- Process subrooms recursively ---
            /*
            if (room.SubRooms != null)
            {
                foreach (var sub in room.SubRooms)
                {
                    string subName = $"{roomName}.{sub.Key}"; // Prefix parent room name
                    ProcessArea(subName, sub.Value);
                }
            }
            */
        }
    }
}
