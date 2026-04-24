using Adventure.Services;
using System.Collections.Concurrent;

namespace Adventure.Services
{
    /// <summary>
    /// Tracks active encounters in-memory for map visualization.
    /// Shows ⚔️ emoji on tiles where players are currently in battle.
    /// 
    /// Key: Discord user ID | Value: (TileId, NpcName)
    /// </summary>
    public static class ActiveEncounterTracker
    {
        /// <summary>
        /// Stores active encounters. Key: userId, Value: (tileId, npcName)
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, (string TileId, string NpcName)> ActiveEncounters = new();

        /// <summary>
        /// Registers a new encounter on a specific tile.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player in battle.</param>
        /// <param name="tileId">The tile ID where the encounter occurs (e.g., "living_room:2,8").</param>
        /// <param name="npcName">The name of the NPC being fought.</param>
        public static void RegisterEncounter(ulong userId, string tileId, string npcName)
        {
            ActiveEncounters[userId] = (tileId, npcName);
            LogService.Info($"[ActiveEncounterTracker.RegisterEncounter] Registered encounter for user {userId} at {tileId} with {npcName}");
        }

        /// <summary>
        /// Removes an encounter from tracking (called when NPC is defeated or player flees).
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        public static void RemoveEncounter(ulong userId)
        {
            if (ActiveEncounters.TryRemove(userId, out var encounter))
            {
                LogService.Info($"[ActiveEncounterTracker.RemoveEncounter] Removed encounter for user {userId} at {encounter.TileId}");
            }
        }

        /// <summary>
        /// Checks if there is an active encounter on a specific tile.
        /// </summary>
        /// <param name="tileId">The tile ID to check (e.g., "living_room:2,8").</param>
        /// <returns>True if an encounter is active on this tile.</returns>
        public static bool HasEncounterOnTile(string tileId)
        {
            return ActiveEncounters.Values.Any(e => e.TileId == tileId);
        }

        /// <summary>
        /// Gets the encounter details for a specific tile.
        /// </summary>
        /// <param name="tileId">The tile ID to check.</param>
        /// <returns>The NPC name if an encounter exists, otherwise null.</returns>
        public static string? GetEncounterNpcName(string tileId)
        {
            var encounter = ActiveEncounters.Values.FirstOrDefault(e => e.TileId == tileId);
            return encounter != default ? encounter.NpcName : null;
        }

        /// <summary>
        /// Gets all active encounter locations in a specific area.
        /// </summary>
        /// <param name="areaId">The area ID to check (e.g., "living_room").</param>
        /// <returns>List of tile positions with active encounters.</returns>
        public static List<string> GetEncountersInArea(string areaId)
        {
            return ActiveEncounters.Values
                .Where(e =>
                {
                    string encounterArea = GetAreaFromTileId(e.TileId);
                    return encounterArea == areaId;
                })
                .Select(e => GetPositionFromTileId(e.TileId))
                .ToList();
        }

        /// <summary>
        /// Gets the encounter tile ID for a specific user.
        /// </summary>
        /// <param name="userId">The Discord user ID.</param>
        /// <returns>The tile ID where the encounter is happening, or null if no encounter.</returns>
        public static string? GetEncounterTileForUser(ulong userId)
        {
            if (ActiveEncounters.TryGetValue(userId, out var encounter))
            {
                return encounter.TileId;
            }
            return null;
        }

        /// <summary>
        /// Clears all encounters (useful for debugging or server restart).
        /// </summary>
        public static void ClearAll()
        {
            int count = ActiveEncounters.Count;
            ActiveEncounters.Clear();
            LogService.Info($"[ActiveEncounterTracker.ClearAll] Cleared {count} active encounters");
        }

        /// <summary>
        /// Extracts the area ID from a tile ID. E.g., "living_room:2,8" → "living_room"
        /// </summary>
        private static string GetAreaFromTileId(string tileId)
        {
            int colonIndex = tileId.IndexOf(':');
            return colonIndex > 0 ? tileId[..colonIndex] : tileId;
        }

        /// <summary>
        /// Extracts the position from a tile ID. E.g., "living_room:2,8" → "2,8"
        /// </summary>
        private static string GetPositionFromTileId(string tileId)
        {
            int colonIndex = tileId.IndexOf(':');
            return colonIndex > 0 ? tileId[(colonIndex + 1)..] : "0,0";
        }
    }
}
