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
        public static Dictionary<string, List<TileModel>> Rooms { get; private set; } = new();

        /// <summary>
        /// Dictionary of room descriptions for display or embeds.
        /// </summary>
        public static Dictionary<string, string> RoomDescriptions { get; private set; } = new();

        public static Dictionary<string, MainHouseRoomModel> RoomModels { get; private set; } = new();


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
                if (mainhouse == null || mainhouse.Rooms == null || mainhouse.Rooms.Count == 0)
                {
                    LogService.Error("[MainHouseLoader] > No rooms found");
                    return null;
                }

                // --- Clear previous data ---
                AllTiles.Clear();
                TileLookup.Clear();
                Rooms.Clear();
                RoomDescriptions.Clear();

                // --- Process all top-level rooms ---
                foreach (var room in mainhouse.Rooms)
                    ProcessRoom(room.Key, room.Value);

                // --- Logging summary ---
                LogService.Info($"[MainHouseLoader] > Loaded {AllTiles.Count} tiles");
                LogService.Info($"[MainHouseLoader] > {RoomDescriptions.Count} rooms loaded");

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
        /// <param name="roomName">The name of the room (parent prefix included for subrooms).</param>
        /// <param name="room">The room model from JSON.</param>
        static void ProcessRoom(string roomName, MainHouseRoomModel room)
        {
            // --- Store tiles for this room ---
            if (room.Tiles != null)
            {
                Rooms[roomName] = room.Tiles;      // Save tiles in Rooms dictionary
                AllTiles.AddRange(room.Tiles);     // Add to flattened list

                LogService.Info($"[MainHouseLoader.ProcessRoom] Loading {roomName}, {room.Tiles.Count} tiles...");

                // --- Build TileLookup for quick access ---
                foreach (var tile in room.Tiles)
                {
                    tile.RoomId = room.Id;
                    if (!string.IsNullOrWhiteSpace(tile.TilePosition))
                    {
                        string key = $"{room.Id}:{tile.TileId}";
                        if (!TileLookup.ContainsKey(key))
                            TileLookup[key] = tile; // Add tile to lookup
                    }
                }
            }

            // --- Store room description ---
            RoomDescriptions[roomName] = room.Description;
            LogService.Info($"[MainHouseLoader.ProcessRoom] Storing description {roomName}");

            // --- Process subrooms recursively ---
            /*
            if (room.SubRooms != null)
            {
                foreach (var sub in room.SubRooms)
                {
                    string subName = $"{roomName}.{sub.Key}"; // Prefix parent room name
                    ProcessRoom(subName, sub.Value);
                }
            }
            */
        }
    }
}
