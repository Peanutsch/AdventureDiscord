using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Models.Player;
using Adventure.Modules;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Encounter;
using Adventure.Quest.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using System.Numerics;
using System.Threading.Tasks;

namespace Adventure.Buttons
{
    public static class ComponentHelpers
    {
        #region === Move Player ===
        public static async Task<bool> MovePlayerAsync(
    SocketInteractionContext context,
    string key,
    bool showTravelAnimation = false,
    bool allowAutoEncounter = true)
        {
            if (!TestHouseLoader.TileLookup.TryGetValue(key, out var targetTile) || targetTile == null)
            {
                LogService.Error($"[ComponentHelpers.MovePlayerAsync] ❌ Target tile '{key}' not found!");
                await context.Interaction.FollowupAsync($"❌ Target tile '{key}' not found.", ephemeral: true);
                return false;
            }

            // Save player position
            LogService.Info($"[ComponentHelpers.MovePlayerAsync] Save location {key} for {context.User.GlobalName}");
            JsonDataManager.UpdatePlayerSavepoint(context.User.Id, key);

            // === AUTO ENCOUNTER CHECK ===
            if (allowAutoEncounter)
            {
                Random rnd = new Random();
                int chance = rnd.Next(1, 100);

                bool isNpc = targetTile.TileType.StartsWith("NPC", StringComparison.OrdinalIgnoreCase);
                bool isTreeEncounter = targetTile.TileName.Equals("Tree", StringComparison.OrdinalIgnoreCase) && chance <= 25;

                if (isNpc || isTreeEncounter)
                {
                    LogService.Info($"[MovePlayerAsync] Auto-encounter triggered on {targetTile.TileName} (chance {chance})");

                    var npc = EncounterRandomizer.NpcRandomizer();
                    if (npc == null)
                    {
                        await context.Interaction.FollowupAsync("⚠️ Could not pick a random creature.");
                        return false;
                    }

                    await ComponentHelpers.TransitionBattleEmbed(context, npc.Name!);

                    SlashCommandHelpers.SetupBattleState(context.User.Id, npc);

                    var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
                    var buttons = SlashCommandHelpers.BuildEncounterButtons(context.User.Id);

                    await context.Interaction.FollowupAsync(embed: embed.Build(), components: buttons.Build());
                    return true;
                }
            }

            // === NORMAL MOVEMENT ===
            if (showTravelAnimation)
                await TransitionTravelEmbed(context, targetTile);

            var embedWalk = EmbedBuildersMap.EmbedWalk(targetTile);
            var components = ButtonBuildersMap.BuildDirectionButtons(targetTile);

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embedWalk.Build();
                msg.Components = components.Build();
            });

            return true;
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
                    .WithTitle("⚔️ GET READY FOR BATTLE ⚔️")
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
                    await MovePlayerAsync(
                        context,
                        destinationTile.TileId,
                        showTravelAnimation: fleeMode == "random",
                        allowAutoEncounter: false
                    );
                    return;
                }

                // Fallback: return to walk mode if no tile is found
                await ComponentInteractions.ReturnToWalkAsync(context);
            }
            catch (Exception ex)
            {
                // Log any unexpected errors and reset player state to walking
                LogService.Error($"[TransitionFleeEmbed] Error during flee relocation: {ex.Message}");
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
                    .WithDescription("You fled as fast as far as you can...")
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
            // Filter out unsafe or non-walkable tiles
            var safeTiles = TestHouseLoader.TileLookup.Values
                .Where(t => !t.TileType.StartsWith("NPC", StringComparison.OrdinalIgnoreCase)
                         && !t.TileName.StartsWith("Wall", StringComparison.OrdinalIgnoreCase)
                         && !t.TileName.StartsWith("TREASURE", StringComparison.OrdinalIgnoreCase)
                         && !t.TileName.StartsWith("Water", StringComparison.OrdinalIgnoreCase)
                         && !t.TileName.StartsWith("Lava", StringComparison.OrdinalIgnoreCase)
                         && !t.TileName.StartsWith("Trap", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Return null if no valid tiles exist
            if (safeTiles.Count == 0) return null;

            // Randomly select a safe tile
            Random rnd = new Random();
            var randomTile = safeTiles[rnd.Next(safeTiles.Count)];
            LogService.Info($"[TransitionFleeEmbed] Player fled randomly to tile: {randomTile.TileId}");
            return randomTile;
        }
        #endregion
    }
}