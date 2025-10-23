using Adventure.Data;
using Adventure.Loaders;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Encounter;
using Adventure.Quest.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Adventure.Buttons
{
    public class ComponentInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        #region === Component Dispatch ===
        /// <summary>
        /// Catch component id when not recognized by ComponentInteraction and call method 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ComponentInteraction("*")]
        public async Task DispatchComponentAction(string id)
        {
            LogService.Info($"[ComponentInteractions.DispatchComponentAction] component ID: {id}");

            if (id.StartsWith("weapon_"))
            {
                await HandleWeaponButton(id);
                return;
            }

            if (id.StartsWith("move_") || id.StartsWith("blocked_"))
            {
                await WalkDirectionHandler(id);
                return;
            }

            if (id.StartsWith("enter:"))
            {
                await EnterTileHandler(id);
                return;
            }

        }
        #endregion

        #region === Battle ===
        [ComponentInteraction("weapon_*")]
        public async Task HandleWeaponButton(string weaponId)
        {
            LogService.Info($"[ComponentInteractions.HandleWeaponButton] > Recieved weaponId: {weaponId}");

            ulong userId = Context.User.Id;

            var state = BattleStateSetup.GetBattleState(Context.User.Id);
            if (state == null)
            {
                await RespondAsync("❌ No active battle found.");
                return;
            }

            var weapon = GameEntityFetcher
                .RetrieveWeaponAttributes(new List<string> { weaponId })
                .FirstOrDefault();

            if (weapon == null)
            {
                LogService.Error($"[ComponentInteractions.HandleWeaponButton] > Weapon ID '{weaponId}' not found.");
                await RespondAsync($"⚠️ Weapon not found: {weaponId}");
                return;
            }

            LogService.Info($"[ComponentInteractions.HandleWeaponButton] > Player choose: {weapon.Name}\n");

            var step = EncounterBattleStepsSetup.GetStep(userId);
            LogService.Info($"[HandleWeaponButton] Current battle step: {step}");

            // Direct call BattleEngine.HandleStepBattle
            await EncounterBattleStepsSetup.HandleStepBattle(Context.Interaction, weaponId);
        }

        [ComponentInteraction("btn_attack")]
        public async Task ButtonAttackHandler()
        {
            await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "attack", "none");
        }

        [ComponentInteraction("btn_flee")]
        public async Task ButtonFleeHandler()
        {
            try
            {
                await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "flee", "none");
            }
            catch (Exception ex)
            {
                LogService.Info($"[ButtonFleeHandler] UpdateAsync failed, fallback to FollowupAsync. {ex.Message}");
                var battleEmbed = new EmbedBuilder().WithDescription("You tried to flee...");
                await Context.Interaction.FollowupAsync(embed: battleEmbed.Build());
            }
        }

        [ComponentInteraction("battle_continue_*")]
        public async Task ContinueBattleHandler(string userIdRaw)
        {
            if (Context.User.Id.ToString() != userIdRaw)
            {
                await RespondAsync("⚠️ You cannot control this battle!", ephemeral: true);
                return;
            }

            EncounterBattleStepsSetup.SetStep(Context.User.Id, EncounterBattleStepsSetup.StepWeaponChoice);

            try
            {
                await EmbedBuildersEncounter.EmbedPreBattle((SocketMessageComponent)Context.Interaction);
            }
            catch (Exception ex)
            {
                LogService.Error($"[ContinueBattleHandler] UpdateAsync failed, fallback to FollowupAsync. {ex.Message}");
                var state = BattleStateSetup.GetBattleState(Context.User.Id);
                var embed = new EmbedBuilder()
                    .WithTitle("Choose your weapon again!")
                    .WithDescription("Previous interaction could not be updated.")
                    .WithColor(Color.Blue);
                await Context.Interaction.FollowupAsync(embed: embed.Build());
            }
        }

        [ComponentInteraction("battle_flee_*")]
        public async Task FleeBattleHandler(string userIdRaw)
        {
            if (Context.User.Id.ToString() != userIdRaw)
            {
                await RespondAsync("⚠️ You cannot control this battle...", ephemeral: true);
                return;
            }

            try
            {
                await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "flee", "none");
            }
            catch (Exception ex)
            {
                LogService.Error($"[FleeBattleHandler] UpdateAsync failed, fallback to FollowupAsync. {ex.Message}");
                var embed = new EmbedBuilder().WithDescription("You tried to flee...");
                await Context.Interaction.FollowupAsync(embed: embed.Build());
            }
        }
        #endregion

        #region === Walk Direction Handler ===
        /// <summary>
        /// Handles directional movement button interactions from the /walk command.
        /// Ensures the player moves to the correct target tile and updates the message with the new embed and buttons.
        /// Prevents multiple responses to the same interaction by using a single response path.
        /// </summary>
        /// <param name="data">Button custom ID data in the format "direction:tileId".</param>
        [ComponentInteraction("move_*")]
        public async Task WalkDirectionHandler(string data)
        {
            try
            {
                // Split the button data into direction and tileId.
                // Example: "east:Room2:tile_1_2"
                var parts = data.Split(':', 2);
                if (parts.Length != 2)
                {
                    // Respond immediately if the button data is invalid.
                    await Context.Interaction.RespondAsync("⚠️ Invalid button data.", ephemeral: true);
                    return;
                }

                string direction = parts[0];
                string targetTileId = parts[1];

                LogService.Info($"[WalkDirectionHandler] direction: {direction}, targetTileId: {targetTileId}");

                // Try to find the target tile in the lookup dictionary.
                if (!MainHouseLoader.TileLookup.TryGetValue(targetTileId, out var targetTile) || targetTile == null)
                {
                    await Context.Interaction.RespondAsync($"❌ Tile '{targetTileId}' not found.", ephemeral: true);
                    return;
                }

                // Defer the interaction, allowing time for processing before updating the original response.
                // This prevents the 3-second Discord timeout.
                await Context.Interaction.DeferAsync();

                // Save new tile as PlayerModel.Savepoint
                LogService.Info($"Saving new savepoint for {Context.User.GlobalName}/{Context.User.Id}, savepoint: {targetTileId}.");
                JsonDataManager.UpdatePlayerSavepoint(Context.User.Id, targetTileId);

                // Build the embed (visual representation of the new room state).
                var embed = EmbedBuildersMap.EmbedWalk(targetTile);

                // Build the movement buttons based on available exits.
                // If there are no exits, add a fallback "Break" button.
                var components = EmbedBuildersMap.BuildDirectionButtons(targetTile)
                    ?? new ComponentBuilder().WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);

                // Safely update the original message with the new embed and direction buttons.
                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Embed = embed.Build();
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                // Log the exception and attempt to follow up with an ephemeral error message.
                LogService.Error($"[WalkDirectionHandler] Exception:\n{ex}");

                try
                {
                    // Use FollowupAsync here because DeferAsync was already called.
                    await Context.Interaction.FollowupAsync("❌ Something went wrong while moving.", ephemeral: true);
                }
                catch (InvalidOperationException)
                {
                    // If FollowupAsync fails (e.g., if the interaction wasn't deferred), ignore to prevent a crash.
                }
            }
        }
        #endregion

        #region Embed for Moving to Other Area
        /// <summary>
        /// Displays a short travel transition embed before entering the next area.
        /// </summary>
        public async Task TransferAnimationEmbed(string targetTileId)
        {
            // Split targetTileId in "roomName" and "tile_{row}_{col}"
            var parts = targetTileId.Split(':');
            var roomName = parts[0];
            //var targetTile = parts[1];

            // --- Show a temporary "Moving..." embed to simulate travel ---
            await Context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("🚶 Moving...")
                    .WithDescription($"Walking to **{roomName}**...")
                    .WithColor(Color.Orange)
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            // --- Simulate travel time ---
            await Task.Delay(1500);
        }
        #endregion

        #region === Enter Button Handler ===
        /// <summary>
        /// Handles the "Enter" button interaction when a player chooses to enter a connected room or tile.
        /// Loads the target tile from MainHouseLoader.TileLookup and updates the embed and buttons accordingly.
        /// </summary>
        [ComponentInteraction("enter:*")]
        public async Task EnterTileHandler(string data)
        {
            LogService.Info($"Received data: {data}");
            try
            {
                // Split the button data: "enter:" and "{roomName}:tile_{row}_{col}"
                var parts = data.Split(":");
                /*
                if (parts.Length != 3)
                {
                    await Context.Interaction.RespondAsync("⚠️ Invalid button data.", ephemeral: false);
                    return;
                }
                */

                // --- Defer the interaction to acknowledge it immediately ---
                await Context.Interaction.DeferAsync();

                var roomName = parts[0]; // e.g., "Room 2"
                var tileId = parts[1];   // e.g. "tile_1_1"
                var key = $"{roomName}:{tileId}";
                LogService.Info($"[EnterTileHandler] data: {data}, Room: {roomName}, tileId: {tileId}, key: {key}");

                // --- Attempt to retrieve the target tile from the lookup dictionary ---
                if (!MainHouseLoader.TileLookup.TryGetValue(key, out var targetTile))
                {
                    LogService.Error($"[ComponentInteractions.EnterTileHandler] ❌ With key {key } target tile '{targetTile}' not found.");
                    await Context.Interaction.FollowupAsync($"With key {key} target tile '{targetTile}' not found.", ephemeral: false);
                    return;
                }

                // Save new tile as PlayerModel.Savepoint
                LogService.Info($"Saving new savepoint for {Context.User.GlobalName}/{Context.User.Id}, savepoint: {targetTile}.");
                JsonDataManager.UpdatePlayerSavepoint(Context.User.Id, key);

                // --- Show a temporary travel embed before updating the view ---
                await TransferAnimationEmbed(roomName);

                // --- Build the updated embed view for the new room/tile ---
                var embed = EmbedBuildersMap.EmbedWalk(targetTile);
                var components = EmbedBuildersMap.BuildDirectionButtons(targetTile);

                // --- Safety fallback if no buttons are returned ---
                if (components == null)
                {
                    components = new ComponentBuilder()
                        .WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);
                }

                // --- Update the message with the new room embed and navigation buttons ---
                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Embed = embed.Build();
                    msg.Components = components.Build();
                });

                LogService.Info($"[EnterTileHandler] Successfully entered {roomName}");
            }
            catch (Exception ex)
            {
                LogService.Error($"[EnterTileHandler] Exception:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while entering the new area.");
            }
        }
        #endregion
    }
}