using Discord.Interactions;
using Discord;
using Adventure.Data;
using Adventure.Services;
using Adventure.Quest.Encounter;
using Discord.WebSocket;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Map;

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
        [ComponentInteraction("_")]
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
        }
        #endregion

        #region === Battle ===
        [ComponentInteraction("weapon_")]
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

        #region === Walk ===
        [ComponentInteraction("move_*")]
        public async Task WalkDirectionHandler(string data)
        {
            try
            {
                await Context.Interaction.DeferAsync();

                // Parse direction and target tile
                var parts = data.Split(':');
                if (parts.Length != 2)
                {
                    await RespondAsync("⚠️ Invalid button data.", ephemeral: true);
                    return;
                }

                string direction = parts[0];
                string targetTileId = parts[1];

                LogService.Info($"[WalkDirectionHandler] direction: {direction}, targetTileId: {targetTileId}");

                var targetTile = GameData.MainHouse?.FirstOrDefault(m => m.TileId == targetTileId);
                if (targetTile == null)
                {
                    await RespondAsync($"❌ Tile '{targetTileId}' not found.", ephemeral: true);
                    return;
                }

                // Build embed and components safely
                var embed = EmbedBuildersWalk.EmbedWalk(targetTile);
                var components = EmbedBuildersWalk.BuildDirectionButtons(targetTile);

                // Always have a fallback
                if (components == null)
                {
                    components = new ComponentBuilder()
                        .WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);
                }

                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Embed = embed.Build();
                    msg.Components = components.Build();
                });
            }
            catch (Exception ex)
            {
                LogService.Error($"[WalkDirectionHandler] Exception:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while moving.", ephemeral: true);
            }
        }

        #region Try out embed for Moving to Other Room
        /*
        await Context.Interaction.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = new EmbedBuilder()
                .WithTitle("🚶 Moving...")
                .WithDescription("You walk through the area...")
                .WithColor(Color.Orange)
                .Build();

            msg.Components = new ComponentBuilder()
                .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                .Build();
        });

        // --- Simulate traveltime
        await Task.Delay(1200);
        */
        #endregion

        /*
        [ComponentInteraction("move_*:*")]
        public async Task WalkDirectionHandler(string data)
        {
            LogService.Info($"[ComponentInteractions.WalkDirectionHandler] Recieved Id: {data}");
            // Data: row, column
            var parts = data.Split(':');
            string direction = parts[0];
            string targetTileId = parts[1];

            LogService.Info($"[ComponentInteractions.WalkDirectionHandler] direction: {direction} targetTileId: {targetTileId}\n");

            var targetTile = GameData.Maps?.FirstOrDefault(m => m.TileId == targetTileId);
            if (targetTile == null)
            {
                await RespondAsync($"❌ Tile '{targetTileId}' not found.", ephemeral: true);
                return;
            }

            var embed = EmbedBuildersWalk.EmbedWalk(targetTile);
            var components = EmbedBuildersWalk.BuildDirectionButtons(targetTile);
            
            var component = (SocketMessageComponent)Context.Interaction;

            await component.UpdateAsync(msg =>
            {
                msg.Embed = embed.Build();
                msg.Components = components?.Build();
            });

        }
        */
        #endregion
    }
}