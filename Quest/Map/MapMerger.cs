using Adventure.Data;
using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Adventure.Quest.Map
{
    public static class MapMerger
    {
        /// <summary>
        /// Dynamically merges all tile lists (like tiles_Room_1, tiles_Room_2, etc.)
        /// into the corresponding rooms in the TestHouseModel layout.
        /// Uses the tile's "position" (row,col) for placement.
        /// </summary>
        public static void MergeAllRooms(TestHouseModel house, TestRoomTilesModel tilesModel)
        {
            if (house == null || house.Rooms.Count == 0)
            {
                LogService.Error("[MapMerger.MergeAllRooms] House or rooms missing, cannot merge.");
                return;
            }

            if (tilesModel == null)
            {
                LogService.Error("[MapMerger.MergeAllRooms] Tile model is null.");
                return;
            }

            // Zoek alle properties in TestRoomTilesModel die lijsten van TileModel bevatten
            var tileLists = tilesModel
                            .GetType()
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => typeof(IEnumerable<TileModel>).IsAssignableFrom(p.PropertyType))
                            .ToDictionary(
                                p => p.Name,
                                p => (IEnumerable<TileModel>?)p.GetValue(tilesModel)
                            );

            foreach (var (key, tiles) in tileLists)
            {
                if (tiles == null) continue;

                // Property key bv. "tiles_Room_1" → roomKey = "room_1"
                string roomKey = key.ToLower();

                LogService.Info($"[MapMerger.MergeAllRooms] key: {key} roomKey: {roomKey}");

                if (!house.Rooms.ContainsKey(roomKey))
                {
                    LogService.Info($"[MapMerger.MergeAllRooms] Room '{roomKey}' not found in house, skipping.");
                    continue;
                }
                
                LogService.Info($"[MapMerger.MergeAllRooms] Merging {tiles.Count()} tiles into '{roomKey}'...");
                MergeTilesIntoRoom(house.Rooms[roomKey], tiles.ToList());
            }
        }

        /// <summary>
        /// Merges tile overlays into the given room layout.
        /// </summary>
        private static void MergeTilesIntoRoom(RoomData room, List<TileModel> tiles)
        {
            foreach (var tile in tiles)
            {
                if (string.IsNullOrWhiteSpace(tile.TilePosition))
                    continue;

                var parts = tile.TilePosition.Split(',');
                if (parts.Length != 2
                    || !int.TryParse(parts[0], out int row)
                    || !int.TryParse(parts[1], out int col))
                    continue;

                // Convert 1-based JSON coords to 0-based list indices
                int rIndex = row - 1;
                int cIndex = col - 1;

                // Expand layout if needed
                while (room.Layout.Count <= rIndex)
                    room.Layout.Add(new List<string>());

                while (room.Layout[rIndex].Count <= cIndex)
                    room.Layout[rIndex].Add("Floor");

                // Determine overlay type
                if (tile.Overlays != null && tile.Overlays.Count > 0)
                {
                    if (tile.Overlays.Contains("START"))
                        room.Layout[rIndex][cIndex] = "START";
                    else if (tile.Overlays.Contains("DOOR"))
                        room.Layout[rIndex][cIndex] = "Door";
                    else
                        room.Layout[rIndex][cIndex] = tile.Overlays.First();
                }
                else
                {
                    room.Layout[rIndex][cIndex] = "Floor";
                }
            }
        }
    }
}
