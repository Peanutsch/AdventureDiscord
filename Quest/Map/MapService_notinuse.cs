using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Quest.Map
{
    public static class MapService_notinuse
    {
        /// <summary>
        /// Geeft een lijst van beschikbare exits voor een tile, met correcte TileLookup keys.
        /// Formaat: "Direction → tileId", bv: "North → living_room:3,3"
        /// </summary>
        public static List<string> GetExits(string areaId, string tilePosition)
        {
            string key = $"{areaId}:{tilePosition}";
            var exits = new List<string>();

            if (!TestHouseLoader.TileLookup.TryGetValue(key, out var tile))
            {
                LogService.Error($"[MapService.GetExits] Tile '{key}' niet gevonden in TileLookup.");
                return exits;
            }

            // Loop door alle connections van deze tile
            if (tile.Connections != null)
            {
                foreach (var connectionKey in tile.Connections)
                {
                    if (!TestHouseLoader.TileLookup.TryGetValue(connectionKey, out var connectedTile))
                    {
                        LogService.Error($"[MapService.GetExits] Connection key '{connectionKey}' bestaat niet in TileLookup.");
                        continue;
                    }

                    string direction = connectedTile.TileDirectionFrom(tile)!; // bv: "North", "West"
                    if (!string.IsNullOrEmpty(direction))
                        exits.Add($"{direction} → {connectionKey}");
                }
            }

            LogService.Info($"[MapService.GetExits] Tile '{key}' exits: {string.Join(", ", exits)}");
            return exits;
        }
    }
}
