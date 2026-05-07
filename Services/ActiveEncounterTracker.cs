using System.Collections.Concurrent;
using System.Text.Json;
using Adventure.Models.NPC;
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
        #region === Nested Classes ===
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
            public NpcModel? Npc { get; set; }

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
        #endregion

        #region === Private Fields ===
        /// <summary>
        /// Stores active encounters. Key: tileId, Value: EncounterData
        /// </summary>
        private static readonly ConcurrentDictionary<string, EncounterData> ActiveEncounters = new();

        /// <summary>
        /// Mapping of userId to their active encounter tileId for quick lookup
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, string> PlayerToEncounter = new();
        #endregion

        #region === Encounter Registration & Removal ===
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
        #endregion

        #region === Damage Tracking ===
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
        #endregion

        #region === Query Methods ===
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
        #endregion

        #region === Persistence (Save/Load) ===
        #region >>> Persistence DTOs <<<
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
        #endregion

        /// <summary>
        /// Saves all active encounters and player-to-encounter mappings to a JSON file.
        /// Called during bot shutdown to preserve battle state across restarts.
        /// Includes null checks and error handling.
        /// </summary>
        public static async Task SaveEncountersAsync()
        {
            try
            {
                var encounters = ConvertEncountersToDtos();
                var persistenceData = BuildPersistenceData(encounters);
                string filePath = EnsureDataDirectoryExists();

                await WriteEncountersToFileAsync(filePath, persistenceData);
                LogSaveResult(encounters.Count, persistenceData.PlayerToEncounter.Count, filePath);
            }
            catch (Exception ex)
            {
                LogService.Error($"[ActiveEncounterTracker.SaveEncountersAsync] ❌ Failed to save encounters: {ex.Message}\n{ex}");
            }
        }
        #endregion

        #region Save Helpers
        /// <summary>
        /// Converts active encounters to DTOs for serialization.
        /// </summary>
        /// <returns>List of encounter DTOs.</returns>
        private static List<EncounterDataDto> ConvertEncountersToDtos()
        {
            var encounters = new List<EncounterDataDto>();
            foreach (var kvp in ActiveEncounters)
            {
                if (kvp.Value == null)
                {
                    LogService.Info($"[ActiveEncounterTracker.ConvertEncountersToDtos] Null encounter data for tile {kvp.Key}, skipping.");
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
                    ThumbHpNpc_100 = kvp.Value.Npc?.ThumbHpNpc_100 ?? string.Empty,
                    ThumbHpNpc_50 = kvp.Value.Npc?.ThumbHpNpc_50 ?? string.Empty,
                    ThumbHpNpc_10 = kvp.Value.Npc?.ThumbHpNpc_10 ?? string.Empty,
                    ThumbHpNpc_0 = kvp.Value.Npc?.ThumbHpNpc_0 ?? string.Empty
                });
            }
            return encounters;
        }

        /// <summary>
        /// Builds the persistence data object from encounters and player mappings.
        /// </summary>
        /// <param name="encounters">List of encounter DTOs.</param>
        /// <returns>Complete persistence data object.</returns>
        private static PersistenceData BuildPersistenceData(List<EncounterDataDto> encounters)
        {
            return new PersistenceData
            {
                Encounters = encounters,
                PlayerToEncounter = PlayerToEncounter.ToDictionary(x => x.Key, x => x.Value ?? string.Empty),
                SavedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Ensures the data directory exists and returns the full file path.
        /// </summary>
        /// <returns>Full path to the ActiveEncounters.json file.</returns>
        private static string EnsureDataDirectoryExists()
        {
            string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            return Path.Combine(dataDir, "ActiveEncounters.json");
        }

        /// <summary>
        /// Writes persistence data to JSON file asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="persistenceData">Data to serialize.</param>
        private static async Task WriteEncountersToFileAsync(string filePath, PersistenceData persistenceData)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(persistenceData, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Logs the result of the save operation.
        /// </summary>
        /// <param name="encounterCount">Number of encounters saved.</param>
        /// <param name="playerMappingCount">Number of player mappings saved.</param>
        /// <param name="filePath">Path where data was saved.</param>
        private static void LogSaveResult(int encounterCount, int playerMappingCount, string filePath)
        {
            if (encounterCount == 0 && playerMappingCount == 0)
            {
                LogService.Info($"[ActiveEncounterTracker.SaveEncountersAsync] ✅ Cleared all encounters (file updated)");
            }
            else
            {
                LogService.Info($"[ActiveEncounterTracker.SaveEncountersAsync] ✅ Saved {encounterCount} active encounters to {filePath}");
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
                string filePath = GetPersistenceFilePath();

                if (!File.Exists(filePath))
                {
                    LogService.Info("[ActiveEncounterTracker.LoadEncountersAsync] No saved encounters file found. Starting with clean state.");
                    return;
                }

                var persistenceData = await ReadAndDeserializePersistenceDataAsync(filePath);
                if (persistenceData == null)
                    return;

                RestoreEncountersFromDtos(persistenceData.Encounters);
                RestorePlayerMappings(persistenceData.PlayerToEncounter);

                LogLoadResult(persistenceData);
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

        /// <summary>
        /// Gets the full path to the persistence file.
        /// </summary>
        /// <returns>Full file path.</returns>
        private static string GetPersistenceFilePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Data", "ActiveEncounters.json");
        }

        /// <summary>
        /// Reads and deserializes the persistence data from JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>Deserialized persistence data, or null if invalid.</returns>
        private static async Task<PersistenceData?> ReadAndDeserializePersistenceDataAsync(string filePath)
        {
            string json = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                LogService.Info("[ActiveEncounterTracker.ReadAndDeserializePersistenceDataAsync] Encounters file is empty.");
                return null;
            }

            var persistenceData = JsonSerializer.Deserialize<PersistenceData>(json);
            if (persistenceData == null)
            {
                LogService.Info("[ActiveEncounterTracker.ReadAndDeserializePersistenceDataAsync] Failed to deserialize encounters data.");
                return null;
            }

            return persistenceData;
        }

        /// <summary>
        /// Restores encounters from DTOs into active encounters dictionary.
        /// </summary>
        /// <param name="encounterDtos">List of encounter DTOs to restore.</param>
        private static void RestoreEncountersFromDtos(List<EncounterDataDto>? encounterDtos)
        {
            if (encounterDtos == null || encounterDtos.Count == 0)
                return;

            foreach (var encounterDto in encounterDtos)
            {
                if (string.IsNullOrWhiteSpace(encounterDto?.TileId))
                {
                    LogService.Info("[ActiveEncounterTracker.RestoreEncountersFromDtos] Skipping invalid encounter with no TileId.");
                    continue;
                }

                var encounterData = CreateEncounterDataFromDto(encounterDto);

                if (!ActiveEncounters.TryAdd(encounterDto.TileId, encounterData))
                {
                    LogService.Info($"[ActiveEncounterTracker.RestoreEncountersFromDtos] Failed to add encounter at {encounterDto.TileId} (already exists).");
                }
            }
        }

        /// <summary>
        /// Creates an EncounterData object from a DTO.
        /// </summary>
        /// <param name="dto">The encounter DTO.</param>
        /// <returns>Fully initialized EncounterData object.</returns>
        private static EncounterData CreateEncounterDataFromDto(EncounterDataDto dto)
        {
            var basicNpc = new NpcModel
            {
                Name = dto.NpcName ?? string.Empty,
                ThumbHpNpc_100 = dto.ThumbHpNpc_100 ?? string.Empty,
                ThumbHpNpc_50 = dto.ThumbHpNpc_50 ?? string.Empty,
                ThumbHpNpc_10 = dto.ThumbHpNpc_10 ?? string.Empty,
                ThumbHpNpc_0 = dto.ThumbHpNpc_0 ?? string.Empty
            };

            return new EncounterData
            {
                TileId = dto.TileId,
                NpcName = dto.NpcName ?? string.Empty,
                CurrentHitpoints = dto.CurrentHitpoints,
                MaxHitpoints = dto.MaxHitpoints,
                PlayerDamage = new ConcurrentDictionary<ulong, int>(dto.PlayerDamage ?? new Dictionary<ulong, int>()),
                ParticipatingPlayers = dto.ParticipatingPlayers ?? new HashSet<ulong>(),
                Npc = basicNpc
            };
        }

        /// <summary>
        /// Restores player-to-encounter mappings and updates battle sessions.
        /// </summary>
        /// <param name="playerMappings">Dictionary of player to tile mappings.</param>
        private static void RestorePlayerMappings(Dictionary<ulong, string>? playerMappings)
        {
            if (playerMappings == null || playerMappings.Count == 0)
                return;

            foreach (var kvp in playerMappings)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                {
                    LogService.Info($"[ActiveEncounterTracker.RestorePlayerMappings] Skipping player {kvp.Key} with invalid tileId.");
                    continue;
                }

                if (!PlayerToEncounter.TryAdd(kvp.Key, kvp.Value))
                {
                    LogService.Info($"[ActiveEncounterTracker.RestorePlayerMappings] Failed to add player {kvp.Key} mapping (already exists).");
                }
                else
                {
                    UpdatePlayerBattleSession(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Updates a player's battle session with their encounter tile ID.
        /// </summary>
        /// <param name="userId">The player's user ID.</param>
        /// <param name="tileId">The encounter tile ID.</param>
        private static void UpdatePlayerBattleSession(ulong userId, string tileId)
        {
            try
            {
                var session = BattleStateSetup.GetBattleSession(userId);
                if (session != null && session.State != null)
                {
                    session.State.EncounterTileId = tileId;
                    LogService.Info($"[ActiveEncounterTracker.UpdatePlayerBattleSession] Updated EncounterTileId for player {userId} to {tileId}");
                }
            }
            catch (Exception ex)
            {
                LogService.Info($"[ActiveEncounterTracker.UpdatePlayerBattleSession] Could not update session for player {userId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs the result of the load operation.
        /// </summary>
        /// <param name="persistenceData">The loaded persistence data.</param>
        private static void LogLoadResult(PersistenceData persistenceData)
        {
            LogService.Info($"[ActiveEncounterTracker.LoadEncountersAsync] ✅ Loaded {persistenceData.Encounters?.Count ?? 0} encounters and {persistenceData.PlayerToEncounter?.Count ?? 0} player mappings. Saved at: {persistenceData.SavedAt:yyyy-MM-dd HH:mm:ss UTC}");
        }
        #endregion
    }
}
