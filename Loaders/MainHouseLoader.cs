using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Loaders
{
    public static class MainHouseLoader
    {
        // Alle tiles van het huis
        public static List<TileModel> AllTiles { get; private set; } = new();

        // Lookup op basis van unieke key: RoomName:TilePosition
        public static Dictionary<string, TileModel> TileLookup { get; private set; } = new();

        // Rooms dictionary voor makkelijk embed gebruik
        public static Dictionary<string, List<TileModel>> Rooms { get; private set; } = new();

        public static List<TileModel>? Load()
        {
            try
            {
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

                foreach (var room in mainhouse.Rooms)
                {
                    string roomName = room.Key;
                    var tiles = room.Value;

                    LogService.Info($"[MainHouseLoader] > Adding {roomName}: {tiles.Count} tiles");

                    Rooms[roomName] = tiles;
                    allRoomsMainHouse.AddRange(tiles);

                    foreach (var tile in tiles)
                    {
                        if (string.IsNullOrWhiteSpace(tile.TilePosition))
                            continue;

                        string key = $"{roomName}:{tile.TilePosition}";

                        if (!TileLookup.ContainsKey(key))
                            TileLookup[key] = tile;
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
                LogService.Error($"[MainHouseLoader] > Error loading mainhouse.json: {ex.Message}");
                return null;
            }
        }
    }
}
