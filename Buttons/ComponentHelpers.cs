using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Models.Player;
using Adventure.Modules;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Encounter;
using Adventure.Quest.Map;
using Adventure.Quest.Map.HashSets;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using static Adventure.Quest.Battle.Randomizers.EncounterRandomizer;

namespace Adventure.Buttons
{
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
            if (!TryGetTargetTile(key, out var targetTile))
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

            var npc = EncounterRandomizer.NpcRandomizer(CRWeightPreference.Balanced, preference);
            if (npc == null)
            {
                await context.Interaction.FollowupAsync("⚠️ Could not pick a random NPC.");
                return false;
            }

            await TransitionBattleEmbed(context, npc.Name!);
            SlashCommandHelpers.SetupBattleState(context.User.Id, npc);

            var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            var buttons = SlashCommandHelpers.BuildEncounterButtons(context.User.Id);

            await context.Interaction.FollowupAsync(embed: embed.Build(), components: buttons.Build());
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
                await TransitionTravelEmbed(context, targetTile);

            var embedWalk = EmbedBuildersMap.EmbedWalk(targetTile);
            var components = ButtonBuildersMap.BuildDirectionButtons(targetTile);

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embedWalk.Build();
                msg.Components = components.Build();
            });
        }
        #endregion

        #region === Transition Travel Embed ===
        public static async Task TransitionTravelEmbed(SocketInteractionContext context, TileModel targetTile)
        {
            var areaName = TestHouseLoader.AreaLookup.TryGetValue(targetTile.AreaId, out var area)
                ? area.Name
                : targetTile.AreaId;

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("Travel 🏃")
                    .WithDescription($"To the **{areaName}**...")
                    .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286889060175972/iu_.png?ex=6912b139&is=69115fb9&hm=5a328c96b633cf372af46cfb0cfd8a8ffddc22b602562905e69ca47c5c9d492d&")
                    .WithColor(Color.Orange)
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(2500);
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

            // Wait to simulate the fleeing animation
            await Task.Delay(2500);

            try
            {
                // Retrieve or create the player instance based on the current Discord user
                var player = await GetPlayerFromContextAsync(context);

                // Choose a destination tile based on flee mode (random or nearby)
                var destinationTile = fleeMode.Equals("random", StringComparison.OrdinalIgnoreCase)
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
            await context.Interaction.DeferAsync();

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
            var user = SlashCommandHelpers.GetDiscordUser(context, context.User.Id);
            var player = SlashCommandHelpers.GetOrCreatePlayer(user!.Id, user.GlobalName ?? user.Username);

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
            var currentTile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint)
                              ?? SlashCommandHelpers.FindStartTile();

            // Retrieve valid connected neighbor tiles
            var neighbors = currentTile?.Connections?
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
            // Combine all unsafe prefixes into one HashSet for quick O(1) lookup
            var unsafePrefixes = HashSets.NonPassableTiles; // e.g. includes "NPC", "Wall", "TREASURE", etc.

            // Filter tiles: only include those whose Name or Type does NOT start with any unsafe prefix
            var safeTiles = TestHouseLoader.TileLookup.Values
                .Where(t => !unsafePrefixes.Any(prefix =>
                    t.TileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    t.TileType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (safeTiles.Count == 0)
                return null;

            // Randomly select one safe tile
            var rnd = new Random();
            var randomTile = safeTiles[rnd.Next(safeTiles.Count)];

            LogService.Info($"[TransitionFleeEmbed] Player fled randomly to tile: {randomTile.TileId}");
            return randomTile;
        }
        #endregion
    }
}