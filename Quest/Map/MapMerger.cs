using Adventure.Models.Map;
using System;
using System.Collections.Generic;

public static class MapMerger
{
    /// <summary>
    /// Merges tile overlays from TestRoomTilesModel into the layout of TestHouseModel.
    /// Tiles with overlays like START or DOOR overwrite the layout at their position.
    /// </summary>
    /// <param name="house">The house model containing rooms and layouts.</param>
    /// <param name="tiles">The tiles to merge into the layout.</param>
    /// <param name="roomKey">The room key to apply the tiles to (e.g., "room_1").</param>
    public static void MergeTilesIntoLayout(TestHouseModel house, List<TileModel> tiles, string roomKey)
    {
        if (!house.Rooms.TryGetValue(roomKey, out var room))
            throw new ArgumentException($"Room '{roomKey}' not found in house.");

        foreach (var tile in tiles)
        {
            if (string.IsNullOrEmpty(tile.TilePosition))
                continue;

            var parts = tile.TilePosition.Split(',');
            if (parts.Length != 2)
                continue;

            if (!int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col))
                continue;

            // Convert 1-based JSON coordinates to 0-based C# indices
            int rIndex = row - 1;
            int cIndex = col - 1;

            // Ensure the layout is big enough
            while (room.Layout.Count <= rIndex)
                room.Layout.Add(new List<string>());

            while (room.Layout[rIndex].Count <= cIndex)
                room.Layout[rIndex].Add("Floor"); // default

            // Determine cell value based on overlays
            if (tile.Overlays!.Contains("START"))
                room.Layout[rIndex][cIndex] = "START";
            else if (tile.Overlays.Contains("DOOR"))
                room.Layout[rIndex][cIndex] = "Door";
            else
                room.Layout[rIndex][cIndex] = "Floor";
        }
    }
}
