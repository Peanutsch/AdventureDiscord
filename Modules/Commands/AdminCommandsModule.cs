using Adventure.Data;
using Adventure.Loaders;
using Adventure.Services;
using Discord;
using Discord.Interactions;

namespace Adventure.Modules.Commands
{
    /// <summary>
    /// Discord slash command module for administrative functions.
    /// 
    /// This module provides commands for:
    /// - Map data reloading and management
    /// - Game world updates without restarting the application
    /// 
    /// These commands are intended for administrators and developers.
    /// </summary>
    public class AdminCommandsModule : InteractionModuleBase<SocketInteractionContext>
    {
        /// <summary>
        /// Reloads all map data from disk and updates the game state.
        /// 
        /// This command is useful for:
        /// - Updating map content without restarting the application
        /// - Testing map changes in real-time
        /// - Refreshing area and tile data after editing JSON files
        /// 
        /// Admin-only in practice (though not technically restricted here).
        /// </summary>
        /// <remarks>
        /// Process:
        /// 1. Defer the interaction (prevents timeout for slow operations)
        /// 2. Call TestHouseLoader.Load() to read map files from disk
        /// 3. Update GameData.TestHouse with fresh map data
        /// 4. Send success message to user
        /// 5. If error occurs, send error message with exception details
        /// 
        /// Map Files Reloaded:
        /// - testhousetiles.json (tile definitions)
        /// - testhouse.json (areas and layouts)
        /// - testhouselocks.json (lock definitions)
        /// 
        /// Example Usage:
        /// User: "/reload"
        /// Bot: "[INFO] Map is reloaded..."
        /// </remarks>
        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("reload", "Reload map")]
        public async Task SlashcommandReloadMapHandler()
        {
            await DeferAsync();

            LogService.DividerParts(1, "Slashcommand /reload");

            try
            {
                // Load Map data
                GameData.TestHouse = TestHouseLoader.Load();
                await FollowupAsync($"[INFO] Map is reloaded...", ephemeral: false);
            }
            catch (Exception ex)
            {
                await FollowupAsync($"[ERROR] Failed reloading map\n{ex}", ephemeral: false);
            }

            LogService.DividerParts(2, "Slashcommand /reload");
        }
    }
}
