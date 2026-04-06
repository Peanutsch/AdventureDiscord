using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Models.Player;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;

namespace Adventure.Modules
{
    /// <summary>
    /// Discord slash command handler module for game administration and control.
    /// 
    /// This module registers and processes Discord slash commands, providing:
    /// - Map reloading functionality for updating game world data at runtime
    /// - Testing and debugging commands
    /// - Administrative functions for game state management
    /// 
    /// Implements IInteractionModuleBase to integrate with Discord.NET's interaction system.
    /// Each slash command method corresponds to a Discord command users can execute
    /// with a "/" prefix in the Discord client.
    /// 
    /// <remarks>
    /// Registered Commands:
    /// - /reload - Reload map data from disk
    /// 
    /// Disabled Commands (in comments):
    /// - /start - Initialize adventure (unused, left for reference)
    /// - /encounter - Trigger random encounter (testing only)
    /// 
    /// Command Execution Flow:
    /// 1. User types /command in Discord
    /// 2. Discord.NET routes to appropriate handler method
    /// 3. Handler processes command and responds to user
    /// 4. Deferred responses allow async processing
    /// </remarks>
    /// </summary>
    public class SlashCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        #region === Slashcommand "start" (NOT IN USE) ===
        /*
        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task SlashCommandStartHandler()
        {
            var user = Context.Client.GetUser(Context.User.Id);
            if (user != null)
            {
                string username = user.Username;
                string displayName = user.GlobalName ?? user.Username;
                LogService.Info($"[Encounter] Discord user '{displayName}' (ID: {user.Id})");
            }

            // Defer the response to prevent the "No response" error
            await DeferAsync();

            LogService.SessionDivider('=', "START");
            LogService.Info("[AdventureGameModule.SlashCommandStartHandler] > Slash Command /start is executed");

            // Reset inventory to basic inventory: Shordsword and Dagger
            //InventoryStateService.LoadInventory(Context.User.Id);

            // Send a follow-up response after the processing is complete.
            //await FollowupAsync("Slash Command /start is executed");
            await FollowupAsync("Your adventure has begun!");
        }
        */
        #endregion

        #region === Slash Command: /reload ===

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
        #endregion

        #region === Slashcommand "encounter" (NOT IN USE) ===
        // Trigger encounter for testing
        /*
        [SlashCommand("encounter", "Triggers a random encounter")]
        public async Task SlashCommandEncounterHandler()
        {
            await DeferAsync();

            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await RespondAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: Encounter");
            LogService.Info($"[/Encounter] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            var player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            var npc = EncounterRandomizer.NpcRandomizer();

            if (npc == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                return;
            }

            SlashCommandHelpers.SetupBattleState(user.Id, npc);

            var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            var buttons = SlashCommandHelpers.BuildEncounterButtons(user.Id);

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
        */
        #endregion

        #region === Slashcommand "start" ===
        /// <summary>
        /// Handles the /start slash command. Moves the player to the current tile, 
        /// toggles any lock if the tile has a switch, and sends the embed and directional buttons to DM.
        /// </summary>
        [CommandContextType(InteractionContextType.Guild)]
        [SlashCommand("start", "Start your adventure in private message.")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();

            // --- Store the guild channel ID for battle updates ---
            BattlePrivateMessageHelper.SetGuildChannelId(Context.User.Id, Context.Channel.Id);

            // --- Get Discord user object ---
            Discord.IUser? user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "SlashCommand: /start");
            LogService.Info($"[/start] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            // --- Get or create player profile ---
            PlayerModel player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            // --- Determine current tile from player's savepoint ---
            TileModel? tile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint);

            // --- Fallback to START tile if savepoint is invalid ---
            if (tile == null)
            {
                LogService.Info($"[/start] Savepoint '{player.Savepoint}' invalid. Fallback to START tile.");
                tile = SlashCommandHelpers.FindStartTile();

                if (tile != null)
                {
                    // Update player's savepoint to START tile
                    player.Savepoint = $"{tile.AreaId}:{tile.TilePosition}";
                    JsonDataManager.UpdatePlayerSavepoint(Context.User.Id, player.Savepoint);
                    LogService.Info($"[/start] Position saved as new savepoint: {player.Savepoint}");
                }
                else
                {
                    await FollowupAsync("❌ No START tile found in any area. Cannot start.", ephemeral: true);
                    return;
                }
            }

            // --- Toggle lock if the current tile has a switch ---
            if (TestHouseLockService.ToggleLockBySwitch(tile, TestHouseLoader.LockLookup))
            {
                LogService.Info($"[/start] Tile {tile.TileId} switch toggled lock: {tile.LockId}");
            }

            // --- Get name of area for logging ---
            string startArea = TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out TestHouseAreaModel? area)
                ? area.Name
                : "Unknown Area";
            LogService.Info($"[/start] Starting in area: {startArea}, position: {tile.TilePosition}");

            // --- Build embed and directional buttons ---
            EmbedBuilder embed = EmbedBuildersMap.EmbedWalk(tile, Context.User.Id); // Reads current tile state (including lock)
            ComponentBuilder components = ButtonBuildersMap.BuildDirectionButtons(tile);

            // --- Track active player position ---
            await ActivePlayerTracker.UpdatePositionAsync(Context.User.Id, player.Name, tile.TileId);

            // --- Send embed and buttons to DM ---
            IUserMessage? dmMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                Context.Interaction,
                embed.Build(),
                components?.Build());

            if (dmMessage != null)
            {
                LogService.Info("[/start] Adventure map sent to DM.");
            }
            else
            {
                await FollowupAsync("⚠️ Failed to send adventure map to DM.");
                return;
            }

            // --- Notify user in channel that adventure is in DM ---
            await FollowupAsync($"🗺️ {player.Name}, your adventure has started! Check your DMs to explore.");

            LogService.DividerParts(2, "SlashCommand: /start");
        }
        #endregion
    }
}