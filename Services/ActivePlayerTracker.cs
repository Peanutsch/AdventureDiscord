using Adventure.Quest.Battle.BattleEngine;
using Adventure.Services;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace Adventure.Services
{
    /// <summary>
    /// Tracks active player positions in-memory for multiplayer map awareness.
    /// Enables features like:
    /// - Showing other players on the map grid
    /// - Push notifications when someone enters your tile
    /// 
    /// Key: Discord user ID | Value: (PlayerName, TileId)
    /// </summary>
    public static class ActivePlayerTracker
    {
        /// <summary>
        /// Stores active player positions. Key: userId, Value: (playerName, tileId)
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, (string PlayerName, string TileId)> ActivePlayers = new();

        /// <summary>
        /// Updates a player's position and sends a DM notification to other players on the same tile.
        /// </summary>
        /// <param name="userId">The Discord user ID of the moving player.</param>
        /// <param name="playerName">The display name of the moving player.</param>
        /// <param name="tileId">The tile ID the player moved to (e.g., "living_room:2,8").</param>
        public static async Task UpdatePositionAsync(ulong userId, string playerName, string tileId)
        {
            // Get previous position before updating
            ActivePlayers.TryGetValue(userId, out var previousPosition);

            // Update to new position
            ActivePlayers[userId] = (playerName, tileId);
            LogService.Info($"[ActivePlayerTracker.UpdatePositionAsync] {playerName} moved to {tileId}");

            // Skip notification if player didn't actually change tiles
            if (previousPosition.TileId == tileId)
                return;

            // Find other players on the same tile and notify them
            var playersOnTile = GetPlayersOnTile(tileId, excludeUserId: userId);
            foreach (var (otherUserId, otherName) in playersOnTile)
            {
                await SendPlayerArrivedNotificationAsync(otherUserId, playerName, tileId);
            }
        }

        /// <summary>
        /// Gets all active players currently on a specific tile.
        /// </summary>
        /// <param name="tileId">The tile ID to check.</param>
        /// <param name="excludeUserId">Optional user ID to exclude from results.</param>
        /// <returns>List of (userId, playerName) tuples for players on the tile.</returns>
        public static List<(ulong UserId, string PlayerName)> GetPlayersOnTile(string tileId, ulong excludeUserId = 0)
        {
            return ActivePlayers
                .Where(kvp => kvp.Value.TileId == tileId && kvp.Key != excludeUserId)
                .Select(kvp => (kvp.Key, kvp.Value.PlayerName))
                .ToList();
        }

        /// <summary>
        /// Gets all active players in a specific area (matching the area part of the tileId).
        /// </summary>
        /// <param name="areaId">The area ID to check (e.g., "living_room").</param>
        /// <param name="excludeUserId">Optional user ID to exclude from results.</param>
        /// <returns>List of (userId, playerName, tilePosition) tuples.</returns>
        public static List<(ulong UserId, string PlayerName, string TilePosition)> GetPlayersInArea(string areaId, ulong excludeUserId = 0)
        {
            return ActivePlayers
                .Where(kvp =>
                {
                    if (kvp.Key == excludeUserId) return false;
                    string playerArea = GetAreaFromTileId(kvp.Value.TileId);
                    return playerArea == areaId;
                })
                .Select(kvp =>
                {
                    string position = GetPositionFromTileId(kvp.Value.TileId);
                    return (kvp.Key, kvp.Value.PlayerName, position);
                })
                .ToList();
        }

        /// <summary>
        /// Removes a player from the active tracker (e.g., when they go offline).
        /// </summary>
        public static void RemovePlayer(ulong userId)
        {
            ActivePlayers.TryRemove(userId, out _);
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

        /// <summary>
        /// Sends a DM notification to a player that another player has arrived on their tile.
        /// </summary>
        private static async Task SendPlayerArrivedNotificationAsync(ulong targetUserId, string arrivingPlayerName, string tileId)
        {
            try
            {
                DiscordSocketClient? client = BattlePrivateMessageHelper.GetClient();
                if (client == null) return;

                SocketUser? user = client.GetUser(targetUserId);
                if (user == null) return;

                IDMChannel dmChannel = await user.CreateDMChannelAsync();

                Embed embed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("👤 A player has arrived!")
                    .WithDescription($"**{arrivingPlayerName}** has entered your area.")
                    .Build();

                await dmChannel.SendMessageAsync(embed: embed);
                LogService.Info($"[ActivePlayerTracker.SendPlayerArrivedNotificationAsync] Notified {user.Username} that {arrivingPlayerName} arrived at {tileId}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[ActivePlayerTracker.SendPlayerArrivedNotificationAsync] Failed to notify user {targetUserId}: {ex.Message}");
            }
        }
    }
}
