using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Map;
using Adventure.Models.Player;
using Adventure.Modules.Helpers;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Encounter;
using Adventure.Quest.Map;
using Adventure.Quest.Map.HashSets;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using static Adventure.Quest.Battle.Randomizers.EncounterRandomizer;

namespace Adventure.Buttons
{
    /// <summary>
    /// Provides helper methods for handling Discord component interactions (buttons, select menus).
    /// 
    /// This static class handles:
    /// - Player movement across the map with encounter detection
    /// - Tile-based navigation and validation
    /// - Automatic encounter triggers based on tile type and probability
    /// - Normal movement processing and display updates
    /// - Player position persistence
    /// 
    /// The class acts as a bridge between Discord button interactions and the game's
    /// map/encounter systems, managing the complete flow of player movement.
    /// 
    /// <remarks>
    /// Main Responsibilities:
    /// 1. Validate target tiles exist
    /// 2. Save player position to persistent storage
    /// 3. Detect and trigger random encounters
    /// 4. Update Discord embeds with new map state
    /// 5. Handle movement failures gracefully
    /// 
    /// Common Workflow:
    /// User clicks movement button → MovePlayerAsync() called
    ///   → Validate tile exists
    ///   → Save position
    ///   → Check for auto-encounter
    ///   → Update Discord embed
    /// </remarks>
    /// </summary>
    public static class ComponentHelpers
    {
        #region === Move Player ===
        /// <summary>
        /// Handles player movement across the map, including saving their position,
        /// performing automatic encounters, and updating the map display.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <param name="key">The key identifying the target tile to move to.</param>
        /// <param name="showTravelAnimation">Whether to show the travel animation during movement.</param>
        /// <param name="allowAutoEncounter">Whether to allow automatic encounters during movement.</param>
        /// <returns><c>true</c> if movement or encounter was successfully handled; otherwise, <c>false</c>.</returns>
        public static async Task<bool> MovePlayerAsync(SocketInteractionContext context, string key, bool showTravelAnimation = false, bool allowAutoEncounter = true)
        {
            if (!TryGetTargetTile(key, out TileModel? targetTile))
                return await HandleMissingTileAsync(context, key);

            SavePlayerPosition(context, key);

            if (allowAutoEncounter && await HandleTriggerAutoEncounterAsync(context, targetTile!))
                return true;

            await HandleNormalMovementAsync(context, targetTile!, showTravelAnimation);
            return true;
        }
        #endregion

        #region === Move Player Helpers ===
        /// <summary>
        /// Attempts to find a tile in the map based on the provided key.
        /// </summary>
        /// <param name="key">The key identifying the tile.</param>
        /// <param name="targetTile">The found tile, or <c>null</c> if not found.</param>
        /// <returns><c>true</c> if the tile was found; otherwise, <c>false</c>.</returns>
        private static bool TryGetTargetTile(string key, out TileModel? targetTile)
        {
            return TestHouseLoader.TileLookup.TryGetValue(key, out targetTile) && targetTile != null;
        }

        /// <summary>
        /// Handles the scenario where a requested tile could not be found.
        /// Sends an error message and logs the issue.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <param name="key">The missing tile key.</param>
        /// <returns>Always returns <c>false</c> to indicate the failure.</returns>
        private static async Task<bool> HandleMissingTileAsync(SocketInteractionContext context, string key)
        {
            LogService.Error($"[MovePlayerAsync] ❌ Target tile '{key}' not found!");
            await context.Interaction.FollowupAsync($"❌ Target tile '{key}' not found.", ephemeral: true);
            return false;
        }

        /// <summary>
        /// Saves the player’s current position to persistent storage.n
        /// </summary>
        /// <param name="context">The Discord interaction context containing user information.</param>
        /// <param name="key">The key of the tile where the player is located.</param>
        private static void SavePlayerPosition(SocketInteractionContext context, string key)
        {
            LogService.Info($"[MovePlayerAsync] Save location {key} for {context.User.GlobalName}");
            JsonDataManager.UpdatePlayerSavepoint(context.User.Id, key);
        }

        /// <summary>
        /// Determines whether an automatic encounter should be triggered based on tile type and random chance.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <param name="targetTile">The tile where the player is moving.</param>
        /// <returns>
        /// <c>true</c> if an auto-encounter occurs and is handled successfully; otherwise, <c>false</c>.
        /// </returns>
        private static async Task<bool> HandleTriggerAutoEncounterAsync(SocketInteractionContext context, TileModel targetTile)
        {
            Random rnd = new Random();
            int chance = rnd.Next(1, 100);
            int setChanceEncounter = 15; // --> Set change encounter enemy NPC at 15%

            bool isNpc = targetTile.TileType.StartsWith("NPC", StringComparison.OrdinalIgnoreCase);
            bool isForestSouthEncounter = targetTile.TileName.Equals("Tree", StringComparison.OrdinalIgnoreCase) &&
                                    chance <= setChanceEncounter;
            bool isForestNorthEncounter = targetTile.TileName.Equals("Tree2", StringComparison.OrdinalIgnoreCase) &&
                                    chance <= setChanceEncounter;

            LogService.Info($"[ComponentHelpers.TryTriggerAutoEncounterAsync] int chance: {chance}");

            if (isForestSouthEncounter)
                return await HandleAutoEncounterAsync(context, targetTile, CreatureListPreference.Bestiary);

            if (isForestNorthEncounter)
                return await HandleAutoEncounterAsync(context, targetTile, CreatureListPreference.Random);

            if (isNpc)
                return await HandleAutoEncounterAsync(context, targetTile, CreatureListPreference.Humanoids);

            return false;
        }

        /// <summary>
        /// Executes the logic for an automatic encounter, including NPC generation,
        /// battle setup, and displaying the encounter embed.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <param name="tile">The tile on which the encounter occurs.</param>
        /// <param name="preference">The type of creature list to use for randomization.</param>
        /// <returns>
        /// <c>true</c> if the encounter was successfully created and displayed; otherwise, <c>false</c>.
        /// </returns>
        private static async Task<bool> HandleAutoEncounterAsync(SocketInteractionContext context, TileModel tile, CreatureListPreference preference)
        {
            LogService.Info($"[MovePlayerAsync] Auto-encounter triggered on {tile.TileName}");

            Models.NPC.NpcModel? npc = EncounterRandomizer.NpcRandomizer(CRWeightPreference.Balanced, preference);
            if (npc == null)
            {
                await context.Interaction.FollowupAsync("⚠️ Could not pick a random NPC.");
                return false;
            }

            await TransitionBattleEmbed(context, npc.Name!);
            SlashCommandHelpers.SetupBattleState(context.User.Id, npc);

            // Store guild channel ID in battle state for public battle updates
            BattleStateModel encounterState = BattleStateSetup.GetBattleState(context.User.Id);
            encounterState.GuildChannelId = BattlePrivateMessageHelper.GetGuildChannelId(context.User.Id);

            EmbedBuilder embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            ComponentBuilder buttons = SlashCommandHelpers.BuildEncounterButtons(context.User.Id);

            // Send encounter to DM instead of channel
            var dmMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                context.Interaction,
                embed.Build(),
                buttons.Build());

            if (dmMessage != null)
            {
                LogService.Info($"[HandleAutoEncounterAsync] ✅ Storing active message {dmMessage.Id} for user {context.User.Id}");
                BattlePrivateMessageHelper.SetActiveBattleMessage(context.User.Id, dmMessage.Id);
            }
            else
            {
                LogService.Error("[HandleAutoEncounterAsync] ❌ Failed to send encounter to DM");
            }

            // Send encounter notification to guild channel
            if (encounterState.GuildChannelId != 0)
            {
                Embed guildEmbed = EmbedBuildersEncounter.BuildGuildEncounterEmbed(encounterState.Player.Name!, npc).Build();
                await BattlePrivateMessageHelper.SendGuildMessageUpdateAsync(encounterState.GuildChannelId, guildEmbed);
            }

            // Notify user in channel that encounter started
            // await context.Interaction.FollowupAsync($"🎲 **{npc.Name}** encountered! Check your DMs to battle.");
            return true;
        }

        /// <summary>
        /// Handles standard player movement when no encounter occurs.
        /// Updates the map view and optionally displays a travel animation.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <param name="targetTile">The tile to which the player is moving.</param>
        /// <param name="showTravelAnimation">Whether to display the travel animation.</param>
        private static async Task HandleNormalMovementAsync(SocketInteractionContext context, TileModel targetTile, bool showTravelAnimation)
        {
            if (showTravelAnimation)
            {
                await TransitionTravelEmbed(context, targetTile);

                // Send travel notification to guild channel
                ulong guildChannelId = BattlePrivateMessageHelper.GetGuildChannelId(context.User.Id);
                if (guildChannelId != 0)
                {
                    Embed guildEmbed = BuildGuildTravelEmbed(context.User.GlobalName ?? context.User.Username, targetTile).Build();
                    await BattlePrivateMessageHelper.SendGuildMessageUpdateAsync(guildChannelId, guildEmbed);
                }
            }

            string playerName = context.User.GlobalName ?? context.User.Username;
            EmbedBuilder embedWalk = await EmbedBuildersMap.EmbedWalkAsync(targetTile, context.User.Id, playerName);
            ComponentBuilder components = ButtonBuildersMap.BuildDirectionButtons(targetTile);

            // Track active player position and notify others
            await ActivePlayerTracker.UpdatePositionAsync(context.User.Id, playerName, targetTile.TileId);

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embedWalk.Build();
                msg.Components = components.Build();
            });
        }
        #endregion

        #region === Transition Travel Embed ===
        /// <summary>
        /// Builds a travel transition embed for moving between tiles.
        /// </summary>
        /// <param name="targetTile">The tile the player is moving to.</param>
        /// <returns>An EmbedBuilder with the transition embed.</returns>
        private static EmbedBuilder BuildTransitionEmbed(TileModel targetTile)
        {
            string? areaName = TestHouseLoader.AreaLookup.TryGetValue(targetTile.AreaId, out TestHouseAreaModel? area)
                ? area.Name
                : targetTile.AreaId;

            return new EmbedBuilder()
                .WithTitle("Travel 🏃")
                .WithDescription($"To the **{areaName}**...")
                .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286889060175972/iu_.png?ex=6912b139&is=69115fb9&hm=5a328c96b633cf372af46cfb0cfd8a8ffddc22b602562905e69ca47c5c9d492d&")
                .WithColor(Color.Orange);
        }

        /// <summary>
        /// Builds a compact travel notification embed for the guild channel.
        /// </summary>
        /// <param name="playerName">The name of the traveling player.</param>
        /// <param name="targetTile">The tile the player is traveling to.</param>
        /// <returns>An EmbedBuilder with the travel notification.</returns>
        private static EmbedBuilder BuildGuildTravelEmbed(string playerName, TileModel targetTile)
        {
            string areaName = TestHouseLoader.AreaLookup.TryGetValue(targetTile.AreaId, out TestHouseAreaModel? area)
                ? area.Name
                : targetTile.AreaId;

            return new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($"🏃 {playerName} travels to {areaName}");
        }

        /// <summary>
        /// Shows a travel transition embed by updating the current interaction response.
        /// Used for normal tile movement via direction buttons.
        /// </summary>
        public static async Task TransitionTravelEmbed(SocketInteractionContext context, TileModel targetTile)
        {
            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildTransitionEmbed(targetTile).Build();
                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(2500);
        }

        /// <summary>
        /// Shows a travel transition embed as a new DM message.
        /// Used for transitions that need a separate message (e.g., after battle continue).
        /// </summary>
        public static async Task<IUserMessage?> TransitionTravelEmbedAsDMAsync(SocketInteractionContext context, TileModel targetTile)
        {
            ComponentBuilder transitionButtons = new ComponentBuilder()
                .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true);

            return await BattlePrivateMessageHelper.SendBattleMessageAsync(
                context.Interaction,
                BuildTransitionEmbed(targetTile).Build(),
                transitionButtons.Build());
        }
        #endregion

        #region === Transition Battle Embed ===
        public static async Task TransitionBattleEmbed(SocketInteractionContext context, string npc)
        {
            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("⚔️ YOU ENCOUNTER AN ENEMY! ⚔️")
                    .WithDescription($"Get ready to fight a **{npc.ToUpper()}**...")
                    .WithColor(Color.Red)
                    .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286545307598969/iu_.png?ex=6912b0e7&is=69115f67&hm=78332d8954422f6b3a261847abea4eba4d30ffa38e10fe9b92da4a03949940ef&")
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(2500);
        }
        #endregion

        #region === Transition Flee Embed ===
        /// <summary>
        /// Handles the transition when a player chooses to flee from battle.
        /// Displays a "fleeing" embed, waits briefly, then moves the player to a nearby or random safe tile.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <param name="fleeMode">Determines the type of fleeing behavior ("nearby" or "random").</param>
        public static async Task TransitionFleeEmbed(SocketInteractionContext context, string fleeMode = "nearby")
        {
            // Display initial flee embed with waiting message
            await ShowFleeingEmbedAsync(context);

            // Send flee notification to guild channel
            ulong guildChannelId = BattlePrivateMessageHelper.GetGuildChannelId(context.User.Id);
            if (guildChannelId != 0)
            {
                BattleStateModel? state = BattleStateSetup.GetBattleState(context.User.Id);
                string playerName = context.User.GlobalName ?? context.User.Username;
                string npcName = state?.Npc?.Name ?? "unknown creature";
                Embed guildEmbed = EmbedBuildersEncounter.BuildGuildFleeEmbed(playerName, npcName).Build();
                await BattlePrivateMessageHelper.SendGuildMessageUpdateAsync(guildChannelId, guildEmbed);
            }

            // Wait to simulate the fleeing animation
            await Task.Delay(2500);

            try
            {
                // Retrieve or create the player instance based on the current Discord user
                PlayerModel player = await GetPlayerFromContextAsync(context);

                // Choose a destination tile based on flee mode (random or nearby)
                TileModel? destinationTile = fleeMode.Equals("random", StringComparison.OrdinalIgnoreCase)
                    ? GetRandomSafeTile()
                    : GetRandomNeighborTile(player);

                // If a valid destination is found, move the player there
                if (destinationTile != null)
                {
                    await MovePlayerAsync(context, destinationTile.TileId, showTravelAnimation: fleeMode == "random", allowAutoEncounter: false);
                    return;
                }

                // Fallback: return to walk mode if no tile is found
                await ComponentInteractions.ReturnToWalkAsync(context);
            }
            catch (Exception ex)
            {
                // Log any unexpected errors and reset player state to walking
                LogService.Error($"[TransitionFleeEmbed] Error during flee relocation:\n{ex.Message}");
                await ComponentInteractions.ReturnToWalkAsync(context);
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Displays an embed indicating that the player is fleeing, with a temporary disabled button.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        private static async Task ShowFleeingEmbedAsync(SocketInteractionContext context)
        {
            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("🏃 Flee 🏃")
                    .WithDescription("You flee as fast and as far as you can...")
                    .WithColor(Color.Orange)
                    .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286170718371900/iu_.png")
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });
        }

        /// <summary>
        /// Retrieves the <see cref="PlayerModel"/> object linked to the Discord user.
        /// If no player exists, a new one is created.
        /// </summary>
        /// <param name="context">The Discord interaction context.</param>
        /// <returns>A task containing the associated player object.</returns>
        private static Task<PlayerModel> GetPlayerFromContextAsync(SocketInteractionContext context)
        {
            IUser? user = SlashCommandHelpers.GetDiscordUser(context, context.User.Id);
            PlayerModel player = SlashCommandHelpers.GetOrCreatePlayer(user!.Id, user.GlobalName ?? user.Username);

            // Wrap the result in a completed Task to satisfy the Task<PlayerModel> return type
            return Task.FromResult(player);
        }

        /// <summary>
        /// Selects a random neighboring tile connected to the player's current position.
        /// </summary>
        /// <param name="player">The current player object.</param>
        /// <returns>A random neighboring tile, or null if none are available.</returns>
        private static TileModel? GetRandomNeighborTile(PlayerModel player)
        {
            // Get the current tile or fallback to the start tile
            TileModel? currentTile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint)
                              ?? SlashCommandHelpers.FindStartTile();

            // Retrieve valid connected neighbor tiles
            List<string>? neighbors = currentTile?.Connections?
                .Where(id => TestHouseLoader.TileLookup.ContainsKey(id))
                .ToList();

            // Randomly pick one of the valid neighbor tiles
            if (neighbors is { Count: > 0 })
            {
                Random rnd = new Random();
                string randomNeighbor = neighbors[rnd.Next(neighbors.Count)];
                LogService.Info($"[TransitionFleeEmbed] Player fled to nearby tile: {randomNeighbor}");
                return TestHouseLoader.TileLookup[randomNeighbor];
            }

            return null;
        }

        /// <summary>
        /// Selects a random safe tile in the world — avoiding hazards, walls, and non-walkable areas.
        /// </summary>
        /// <returns>A random safe tile, or null if none are found.</returns>
        private static TileModel? GetRandomSafeTile()
        {
            // Define unsafe prefixes that indicate non-walkable or hazardous tiles
            HashSet<string>? unsafePrefixes = HashSets.NonPassableTiles; // e.g. includes "NPC", "Wall", "TREASURE", etc.

            // Filter tiles: only include those whose Name or Type does NOT start with any unsafe prefix
            List<TileModel> safeTiles = TestHouseLoader.TileLookup.Values
                .Where(t => !unsafePrefixes.Any(prefix =>
                    t.TileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    t.TileType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (safeTiles.Count == 0)
                return null;

            // Randomly select one safe tile
            Random rnd = new Random();
            TileModel randomTile = safeTiles[rnd.Next(safeTiles.Count)];

            LogService.Info($"[TransitionFleeEmbed] Player fled randomly to tile: {randomTile.TileId}");
            return randomTile;
        }
        #endregion
    }
}