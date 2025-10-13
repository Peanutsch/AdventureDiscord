using Adventure.Models.Map;
using Adventure.Services;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Map
{
    public class WalkAMap4Tiles
    {
        public static void StartWalk(MapModel map)
        {
            string id = map.Id;
            string mapName = map.MapName;

            LogService.Info($"[WalkAMap4Tiles.StartWalk] Id: {id} | Name: {mapName}");

            var connections = map.MapConnections;
            if (connections == null)
            {
                LogService.Error($"[WalkAMap4Tiles.StartWalk] No connections found for {mapName}");
                return;
            }

            var possibleConnections = new Dictionary<string, string?>
            {
                { "North", connections.North },
                { "East", connections.East },
                { "South", connections.South },
                { "West", connections.West }
            }
            .Where(c => !string.IsNullOrEmpty(c.Value))
            .ToList();

            if (!possibleConnections.Any())
            {
                LogService.Info($"[WalkAMap4Tiles.StartWalk] {mapName} has no available connections.");
                return;
            }

            foreach (var connection in possibleConnections)
            {
                LogService.Info($"[WalkAMap4Tiles.StartWalk] {connection.Key} → {connection.Value}");
            }
        }
    }
}
