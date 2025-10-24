using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Modules;
using Adventure.Quest.Map;

namespace Adventure.Quest.Map
{
    public class MovementHelper
    {
        // Track current player position (you can store this in PlayerData instead)
        private int playerX = 0;
        private int playerY = 0;

        /// <summary>
        /// Moves the player in the specified direction and renders the updated map.
        /// </summary>
        /// <param name="direction">Direction of movement (north, south, east, west).</param>
        /// <param name="currentTile">The current tile being displayed.</param>
        public string MovePlayer(string direction, TileModel currentTile)
        {
            // Adjust player coordinates based on direction
            switch (direction.ToLower())
            {
                case "north": playerY = Math.Max(0, playerY - 1); break;
                case "south": playerY = Math.Min(currentTile.TileGrid.Count - 1, playerY + 1); break;
                case "west": playerX = Math.Max(0, playerX - 1); break;
                case "east": playerX = Math.Min(currentTile.TileGrid[0].Count - 1, playerX + 1); break;
            }

            Console.WriteLine($"[Info] Player moved {direction.ToUpper()} to ({playerX}, {playerY})");

            // Get the area layout from AreaLookup using the tile's AreaId
            var areaId = currentTile.AreaId;
            var areaLayout = TestHouseLoader.AreaLookup[areaId].Layout;

            // Render the map
            string renderedMap = TileUI.RenderTileGrid(areaLayout);

            return renderedMap;
        }
    }
}