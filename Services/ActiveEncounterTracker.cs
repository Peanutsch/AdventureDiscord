using System.Collections.Concurrent;
using System.Text.Json;
using Adventure.Quest.Battle.BattleEngine;

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
        /// Thread-safe for concurrent attacks from multiple players.
        /// </summary>
        public class EncounterData
        {
            private readonly Lock _hpLock = new Lock();

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

            /// <summary>
            /// Thread-safe method to apply damage and update NPC HP.
            /// Prevents race conditions when multiple players attack simultaneously.
            /// </summary>
            /// <param name="userId">Player dealing damage</param>
            /// <param name="damage">Amount of damage dealt</param>
            /// <returns>Tuple of (new NPC HP, was NPC defeated)</returns>
            public (int newHp, bool isDefeated) ApplyDamage(ulong userId, int damage)
            {
                lock (_hpLock)
                {
                    // Record player damage
                    PlayerDamage.AddOrUpdate(userId, damage, (_, existingDamage) => existingDamage + damage);

                    // Apply damage to NPC
                    int oldHp = CurrentHitpoints;
                    CurrentHitpoints = Math.Max(0, CurrentHitpoints - damage);
                    bool isDefeated = CurrentHitpoints <= 0 && oldHp > 0;

                    return (CurrentHitpoints, isDefeated);
                }
            }
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
        /// Thread-safe for concurrent attacks.
        /// </summary>
        /// <returns>Tuple of (new NPC HP, was NPC defeated by this hit)</returns>
        public static (int newHp, bool isDefeated) RecordDamage(ulong userId, string tileId, int damage)
        {
            if (ActiveEncounters.TryGetValue(tileId, out var encounter))
            {
                var (newHp, isDefeated) = encounter.ApplyDamage(userId, damage);
                LogService.Info($"[ActiveEncounterTracker.RecordDamage] User {userId} dealt {damage} damage at {tileId}. Total: {encounter.PlayerDamage[userId]}, NPC HP: {newHp}/{encounter.MaxHitpoints}, Defeated: {isDefeated}");
                return (newHp, isDefeated);
            }
            return (0, false);
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
        /// Removes an entire encounter from tracking (called when NPC is defeated).
        /// Thread-safe: only the first caller gets true, preventing duplicate victory processing.
        /// </summary>
        /// <returns>True if encounter was removed, false if already removed by another player</returns>
        public static bool TryRemoveEncounter(string tileId)
        {
            if (ActiveEncounters.TryRemove(tileId, out var encounter))
            {
                // Remove player mappings
                foreach (var playerId in encounter.ParticipatingPlayers)
                {
                    PlayerToEncounter.TryRemove(playerId, out _);
                }
                LogService.Info($"[ActiveEncounterTracker.TryRemoveEncounter] Removed encounter at {tileId} with {encounter.ParticipatingPlayers.Count} participants");
                return true;
            }
            LogService.Info($"[ActiveEncounterTracker.TryRemoveEncounter] Encounter at {tileId} already removed (concurrent victory)");
            return false;
        }

        /// <summary>
        /// Removes an entire encounter from tracking (called when NPC is defeated).
        /// </summary>
        public static void RemoveEncounter(string tileId)
        {
            TryRemoveEncounter(tileId);
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
        /// Gets the encounter tile ID for a specific user.
        /// </summary>
        public static string? GetEncounterTileForUser(ulong userId)
        {
            PlayerToEncounter.TryGetValue(userId, out var tileId);
            return tileId;
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

        #region === Persistence (Save/Load) ===

        /// <summary>
        /// Data Transfer Object for serializing encounter data to JSON.
        /// Used to persist active encounters and player mappings to disk.
        /// </summary>
        private class EncounterDataDto
        {
            public string TileId { get; set; } = string.Empty;
            public string NpcName { get; set; } = string.Empty;
            public int CurrentHitpoints { get; set; }
            public int MaxHitpoints { get; set; }
            public Dictionary<ulong, int> PlayerDamage { get; set; } = new();
            public HashSet<ulong> ParticipatingPlayers { get; set; } = new();

            // NPC thumbnail URLs for embed display
            public string ThumbHpNpc_100 { get; set; } = string.Empty;
            public string ThumbHpNpc_50 { get; set; } = string.Empty;
            public string ThumbHpNpc_10 { get; set; } = string.Empty;
            public string ThumbHpNpc_0 { get; set; } = string.Empty;
        }

        /// <summary>
        /// Root object for saving all active encounters and player mappings.
        /// </summary>
        private class PersistenceData
        {
            public List<EncounterDataDto> Encounters { get; set; } = new();
            public Dictionary<ulong, string> PlayerToEncounter { get; set; } = new();
            public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Saves all active encounters and player-to-encounter mappings to a JSON file.
        /// Called during bot shutdown to preserve battle state across restarts.
        /// Includes null checks and error handling.
        /// </summary>
        public static async Task SaveEncountersAsync()
        {
            try
            {
                // Convert ConcurrentDictionary to DTO format for serialization
                var encounters = new List<EncounterDataDto>();
                foreach (var kvp in ActiveEncounters)
                {
                    if (kvp.Value == null)
                    {
                        LogService.Info($"[ActiveEncounterTracker.SaveEncountersAsync] Null encounter data for tile {kvp.Key}, skipping.");
                        continue;
                    }

                    encounters.Add(new EncounterDataDto
                    {
                        TileId = kvp.Value.TileId ?? string.Empty,
                        NpcName = kvp.Value.NpcName ?? string.Empty,
                        CurrentHitpoints = kvp.Value.CurrentHitpoints,
                        MaxHitpoints = kvp.Value.MaxHitpoints,
                        PlayerDamage = kvp.Value.PlayerDamage?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<ulong, int>(),
                        ParticipatingPlayers = kvp.Value.ParticipatingPlayers ?? new HashSet<ulong>(),
                        // Save NPC thumbnail URLs for embed display
                        ThumbHpNpc_100 = kvp.Value.Npc?.ThumbHpNpc_100 ?? string.Empty,
                        ThumbHpNpc_50 = kvp.Value.Npc?.ThumbHpNpc_50 ?? string.Empty,
                        ThumbHpNpc_10 = kvp.Value.Npc?.ThumbHpNpc_10 ?? string.Empty,
                        ThumbHpNpc_0 = kvp.Value.Npc?.ThumbHpNpc_0 ?? string.Empty
                    });
                }

                var persistenceData = new PersistenceData
                {
                    Encounters = encounters,
                    PlayerToEncounter = PlayerToEncounter.ToDictionary(x => x.Key, x => x.Value ?? string.Empty),
                    SavedAt = DateTime.UtcNow
                };

                // Ensure data directory exists
                string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                string filePath = Path.Combine(dataDir, "ActiveEncounters.json");

                // Serialize with indentation for readability
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(persistenceData, options);

                // Write to file asynchronously (always write, even if empty - this clears stale data)
                await File.WriteAllTextAsync(filePath, json);

                if (encounters.Count == 0 && persistenceData.PlayerToEncounter.Count == 0)
                {
                    LogService.Info($"[ActiveEncounterTracker.SaveEncountersAsync] ✅ Cleared all encounters (file updated)");
                }
                else
                {
                    LogService.Info($"[ActiveEncounterTracker.SaveEncountersAsync] ✅ Saved {encounters.Count} active encounters to {filePath}");
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"[ActiveEncounterTracker.SaveEncountersAsync] ❌ Failed to save encounters: {ex.Message}\n{ex}");
            }
        }

        /// <summary>
        /// Loads previously saved encounters and player-to-encounter mappings from a JSON file.
        /// Called during bot startup to restore battle state after crashes or restarts.
        /// Includes comprehensive null checks and error handling.
        /// </summary>
        public static async Task LoadEncountersAsync()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "ActiveEncounters.json");

                // Check if persistence file exists
                if (!File.Exists(filePath))
                {
                    LogService.Info("[ActiveEncounterTracker.LoadEncountersAsync] No saved encounters file found. Starting with clean state.");
                    return;
                }

                // Read and deserialize JSON
                string json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LogService.Info("[ActiveEncounterTracker.LoadEncountersAsync] Encounters file is empty.");
                    return;
                }

                var persistenceData = JsonSerializer.Deserialize<PersistenceData>(json);
                if (persistenceData == null)
                {
                    LogService.Info("[ActiveEncounterTracker.LoadEncountersAsync] Failed to deserialize encounters data.");
                    return;
                }

                // Restore encounters
                if (persistenceData.Encounters != null && persistenceData.Encounters.Count > 0)
                {
                    foreach (var encounterDto in persistenceData.Encounters)
                    {
                        if (string.IsNullOrWhiteSpace(encounterDto?.TileId))
                        {
                            LogService.Info("[ActiveEncounterTracker.LoadEncountersAsync] Skipping invalid encounter with no TileId.");
                            continue;
                        }

                        // Create a basic NPC object with thumbnail URLs for embed display
                        var basicNpc = new Models.NPC.NpcModel
                        {
                            Name = encounterDto.NpcName ?? string.Empty,
                            ThumbHpNpc_100 = encounterDto.ThumbHpNpc_100 ?? string.Empty,
                            ThumbHpNpc_50 = encounterDto.ThumbHpNpc_50 ?? string.Empty,
                            ThumbHpNpc_10 = encounterDto.ThumbHpNpc_10 ?? string.Empty,
                            ThumbHpNpc_0 = encounterDto.ThumbHpNpc_0 ?? string.Empty
                        };

                        var encounterData = new EncounterData
                        {
                            TileId = encounterDto.TileId,
                            NpcName = encounterDto.NpcName ?? string.Empty,
                            CurrentHitpoints = encounterDto.CurrentHitpoints,
                            MaxHitpoints = encounterDto.MaxHitpoints,
                            PlayerDamage = new ConcurrentDictionary<ulong, int>(encounterDto.PlayerDamage ?? new Dictionary<ulong, int>()),
                            ParticipatingPlayers = encounterDto.ParticipatingPlayers ?? new HashSet<ulong>(),
                            Npc = basicNpc
                        };

                        // Add to active encounters
                        if (!ActiveEncounters.TryAdd(encounterDto.TileId, encounterData))
                        {
                            LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] Failed to add encounter at {encounterDto.TileId} (already exists).");
                        }
                    }
                }

                // Restore player-to-encounter mappings
                if (persistenceData.PlayerToEncounter != null && persistenceData.PlayerToEncounter.Count > 0)
                {
                    foreach (var kvp in persistenceData.PlayerToEncounter)
                    {
                        if (string.IsNullOrWhiteSpace(kvp.Value))
                        {
                            LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] Skipping player {kvp.Key} with invalid tileId.");
                            continue;
                        }

                        if (!PlayerToEncounter.TryAdd(kvp.Key, kvp.Value))
                        {
                            LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] Failed to add player {kvp.Key} mapping (already exists).");
                        }
                        else
                        {
                            // Also update the player's BattleSession.State.EncounterTileId for resume functionality
                            try
                            {
                                var session = BattleStateSetup.GetBattleSession(kvp.Key);
                                if (session != null && session.State != null)
                                {
                                    session.State.EncounterTileId = kvp.Value;
                                    LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] Updated EncounterTileId for player {kvp.Key} to {kvp.Value}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] Could not update session for player {kvp.Key}: {ex.Message}");
                            }
                        }
                    }
                }

                LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] ✅ Loaded {persistenceData.Encounters?.Count ?? 0} encounters and {persistenceData.PlayerToEncounter?.Count ?? 0} player mappings. Saved at: {persistenceData.SavedAt:yyyy-MM-dd HH:mm:ss UTC}");
            }
            catch (JsonException ex)
            {
                LogService.Error($"[ActiveEncounterTracker.LoadEncountersAsync] ❌ JSON deserialization error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[ActiveEncounterTracker.LoadEncountersAsync] ❌ Failed to load encounters: {ex.Message}\n{ex}");
            }
        }

        #endregion Persistence
    }
}
