using Adventure.Loaders;
using Adventure.Models.Player;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Adventure.Services
{
    /// <summary>
    /// Handles cleanup of stuck player sessions on bot startup.
    /// 
    /// When the bot restarts, any players who were in an active session
    /// will have their state reset to Idle, allowing them to start new adventures immediately.
    /// </summary>
    public class SessionCleanupService
    {
        private const string PLAYER_DATA_FOLDER = "Data/Player";

        /// <summary>
        /// Scans all player JSON files and resets any non-Idle states back to Idle.
        /// Called on bot startup to clean up stuck sessions from crashes/restarts.
        /// </summary>
        public async Task CleanupAllStuckSessionsAsync()
        {
            try
            {
                LogService.DividerParts(1, "Session Cleanup Service");
                LogService.Info("[SessionCleanupService] Starting stuck session cleanup...");

                if (!Directory.Exists(PLAYER_DATA_FOLDER))
                {
                    LogService.Info("[SessionCleanupService] Player data folder not found. Nothing to cleanup.");
                    LogService.DividerParts(2, "Session Cleanup Service");
                    return;
                }

                // Get all player JSON files
                var playerFiles = Directory.GetFiles(PLAYER_DATA_FOLDER, "*.json");

                if (playerFiles.Length == 0)
                {
                    LogService.Info("[SessionCleanupService] No player files found.");
                    LogService.DividerParts(2, "Session Cleanup Service");
                    return;
                }

                int totalPlayers = playerFiles.Length;
                int cleanedPlayers = 0;

                // Process each player file
                foreach (var filePath in playerFiles)
                {
                    try
                    {
                        // Extract userId from filename (e.g., "123456789.json" -> 123456789)
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (!ulong.TryParse(fileName, out ulong userId))
                        {
                            LogService.Info($"[SessionCleanupService] ⚠️ Invalid player file name: {fileName}");
                            continue;
                        }

                        // Load player
                        PlayerModel? player = JsonDataManager.LoadPlayerFromJson(userId);
                        if (player == null)
                        {
                            LogService.Info($"[SessionCleanupService] ⚠️ Failed to load player {userId}");
                            continue;
                        }

                        // Check if cleanup needed
                        if (player.CurrentState != PlayerState.Idle)
                        {
                            LogService.Info($"[SessionCleanupService] Resetting stuck session for player {userId} ({player.Name}) from {player.CurrentState} → Idle");
                            
                            // Reset state and save
                            player.CurrentState = PlayerState.Idle;
                            player.LastActivityTime = DateTime.UtcNow;
                            JsonDataManager.UpdatePlayerState(userId, PlayerState.Idle);
                            JsonDataManager.UpdatePlayerLastActivityTime(userId);
                            
                            cleanedPlayers++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"[SessionCleanupService] Error processing player file {filePath}: {ex.Message}");
                    }
                }

                // Summary
                LogService.Info($"[SessionCleanupService] ✅ Cleanup complete. Cleaned {cleanedPlayers}/{totalPlayers} stuck sessions.");
                LogService.DividerParts(2, "Session Cleanup Service");
            }
            catch (Exception ex)
            {
                LogService.Error($"[SessionCleanupService] Critical error during cleanup: {ex.Message}");
                LogService.DividerParts(2, "Session Cleanup Service");
            }

            await Task.CompletedTask;
        }
    }
}
