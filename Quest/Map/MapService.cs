using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Quest.Map
{
    public static class MapService
    {
        // Helper om richting te bepalen tussen twee tiles
        public static string? DetermineDirection(TileModel current, TileModel target)
        {
            var partsOrigin = current.TilePosition.Split(',');
            var partsTarget = target.TilePosition.Split(',');

            int row0 = int.Parse(partsOrigin[0]);
            int col0 = int.Parse(partsOrigin[1]);
            int row1 = int.Parse(partsTarget[0]);
            int col1 = int.Parse(partsTarget[1]);

            if (row1 == row0 - 1 && col1 == col0) return "North";
            if (row1 == row0 + 1 && col1 == col0) return "South";
            if (row1 == row0 && col1 == col0 - 1) return "West";
            if (row1 == row0 && col1 == col0 + 1) return "East";

            return null;
        }
    }
}
