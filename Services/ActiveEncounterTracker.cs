using Adventure.Services;
using System.Collections.Concurrent;

namespace Adventure.Services
{
    /// <summary>
    /// Tracks active encounters in-memory for map visualization and multiplayer support.
    /// Shows ⚔️ emoji on tiles where players are currently in battle.
    /// Supports multiple players fighting the same NPC with damage tracking.
    /// 
    /// Key: TileId | Value: EncounterData (NPC name, HP, damage per player)
    /// </summary>
    public static class ActiveEncounterTracker
    {
        /// <summary>
        /// Data structure for tracking an active encounter with multiplayer support.
        /// </summary>
        public class EncounterData
        {
            public string TileId { get; set; } = string.Empty;
            public string NpcName { get; set; } = string.Empty;
            public int CurrentHitpoints { get; set; }
            public int MaxHitpoints { get; set; }

            /// <summary>
            /// Full NPC model with all data (thumbnails, CR, weapons, etc.)
            /// Shared across all players in this encounter
            /// </summary>
            public Adventure.Models.NPC.NpcModel? Npc { get; set; }

            /// <summary>
            /// Tracks total damage dealt by each player. Key: userId, Value: total damage
            /// </summary>
            public ConcurrentDictionary<ulong, int> PlayerDamage { get; set; } = new();

            /// <summary>
            /// List of all player IDs participating in this encounter
            /// </summary>
            public HashSet<ulong> ParticipatingPlayers { get; set; } = new();
        }

        /// <summary>
        /// Stores active encounters. Key: tileId, Value: EncounterData
        /// </summary>
        private static readonly ConcurrentDictionary<string, EncounterData> ActiveEncounters = new();

        /// <summary>
        /// Mapping of userId to their active encounter tileId for quick lookup
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, string> PlayerToEncounter = new();

        /// <summary>
        /// Registers a new encounter on a specific tile, or adds a player to an existing encounter.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player in battle.</param>
        /// <param name="tileId">The tile ID where the encounter occurs (e.g., "living_room:2,8").</param>
        /// <param name="npcName">The name of the NPC being fought.</param>
        /// <param name="maxHitpoints">The maximum HP of the NPC.</param>
        /// <param name="npc">The full NPC model with all data (optional for first player).</param>
        public static void RegisterEncounter(ulong userId, string tileId, string npcName, int maxHitpoints = 0, Adventure.Models.NPC.NpcModel? npc = null)
        {
            var encounterData = ActiveEncounters.GetOrAdd(tileId, _ => new EncounterData
            {
                TileId = tileId,
                NpcName = npcName,
                CurrentHitpoints = maxHitpoints,
                MaxHitpoints = maxHitpoints,
                Npc = npc
            });

            encounterData.ParticipatingPlayers.Add(userId);
            encounterData.PlayerDamage.TryAdd(userId, 0);
            PlayerToEncounter[userId] = tileId;

            LogService.Info($"[ActiveEncounterTracker.RegisterEncounter] User {userId} joined encounter at {tileId} with {npcName}");
        }

        /// <summary>
        /// Records damage dealt by a specific player to the NPC.
        /// </summary>
        public static void RecordDamage(ulong userId, string tileId, int damage)
        {
            if (ActiveEncounters.TryGetValue(tileId, out var encounter))
            {
                encounter.PlayerDamage.AddOrUpdate(userId, damage, (_, existingDamage) => existingDamage + damage);
                encounter.CurrentHitpoints = Math.Max(0, encounter.CurrentHitpoints - damage);
                LogService.Info($"[ActiveEncounterTracker.RecordDamage] User {userId} dealt {damage} damage at {tileId}. Total: {encounter.PlayerDamage[userId]}, NPC HP: {encounter.CurrentHitpoints}/{encounter.MaxHitpoints}");
            }
        }

        /// <summary>
        /// Updates the current HP of the NPC in the encounter.
        /// </summary>
        public static void UpdateNpcHitpoints(string tileId, int currentHp)
        {
            if (ActiveEncounters.TryGetValue(tileId, out var encounter))
            {
                encounter.CurrentHitpoints = currentHp;
            }
        }

        /// <summary>
        /// Gets damage statistics for XP distribution when NPC is defeated.
        /// Returns dictionary of userId -> percentage of total damage (0-100).
        /// </summary>
        public static Dictionary<ulong, int> GetDamageRatios(string tileId)
        {
            if (!ActiveEncounters.TryGetValue(tileId, out var encounter))
                return new Dictionary<ulong, int>();

            int totalDamage = encounter.PlayerDamage.Values.Sum();
            if (totalDamage == 0)
                return new Dictionary<ulong, int>();

            var ratios = new Dictionary<ulong, int>();
            foreach (var kvp in encounter.PlayerDamage)
            {
                int percentage = (int)Math.Round((double)kvp.Value / totalDamage * 100);
                ratios[kvp.Key] = percentage;
                LogService.Info($"[ActiveEncounterTracker.GetDamageRatios] User {kvp.Key}: {kvp.Value}/{totalDamage} damage = {percentage}% XP");
            }

            return ratios;
        }

        /// <summary>
        /// Gets all players participating in an encounter on a specific tile.
        /// </summary>
        public static List<ulong> GetParticipatingPlayers(string tileId)
        {
            if (ActiveEncounters.TryGetValue(tileId, out var encounter))
                return encounter.ParticipatingPlayers.ToList();
            return new List<ulong>();
        }

        /// <summary>
        /// Removes an entire encounter from tracking (called when NPC is defeated).
        /// </summary>
        public static void RemoveEncounter(string tileId)
        {
            if (ActiveEncounters.TryRemove(tileId, out var encounter))
            {
                // Remove player mappings
                foreach (var playerId in encounter.ParticipatingPlayers)
                {
                    PlayerToEncounter.TryRemove(playerId, out _);
                }
                LogService.Info($"[ActiveEncounterTracker.RemoveEncounter] Removed encounter at {tileId} with {encounter.ParticipatingPlayers.Count} participants");
            }
        }

        /// <summary>
        /// Removes a player from an encounter (e.g., when they flee or die).
        /// Does NOT remove the encounter itself - other players can continue.
        /// </summary>
        public static void RemovePlayerFromEncounter(ulong userId)
        {
            if (PlayerToEncounter.TryRemove(userId, out var tileId))
            {
                if (ActiveEncounters.TryGetValue(tileId, out var encounter))
                {
                    encounter.ParticipatingPlayers.Remove(userId);
                    LogService.Info($"[ActiveEncounterTracker.RemovePlayerFromEncounter] User {userId} left encounter at {tileId}. {encounter.ParticipatingPlayers.Count} players remaining");

                    // If no players left, remove entire encounter
                    if (encounter.ParticipatingPlayers.Count == 0)
                    {
                        RemoveEncounter(tileId);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if there is an active encounter on a specific tile.
        /// </summary>
        public static bool HasEncounterOnTile(string tileId)
        {
            return ActiveEncounters.ContainsKey(tileId);
        }

        /// <summary>
        /// Gets the encounter details for a specific tile.
        /// </summary>
        public static EncounterData? GetEncounter(string tileId)
        {
            ActiveEncounters.TryGetValue(tileId, out var encounter);
            return encounter;
        }

        /// <summary>
        /// Gets the encounter NPC name for a specific tile.
        /// </summary>
        public static string? GetEncounterNpcName(string tileId)
        {
            return GetEncounter(tileId)?.NpcName;
        }

        /// <summary>
        /// Gets the encounter tile ID for a specific user.
        /// </summary>
        public static string? GetEncounterTileForUser(ulong userId)
        {
            PlayerToEncounter.TryGetValue(userId, out var tileId);
            return tileId;
        }

        /// <summary>
        /// Gets all active encounter locations in a specific area.
        /// </summary>
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
        /// Clears all encounters (useful for debugging or server restart).
        /// </summary>
        public static void ClearAll()
        {
            int count = ActiveEncounters.Count;
            ActiveEncounters.Clear();
            PlayerToEncounter.Clear();
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
