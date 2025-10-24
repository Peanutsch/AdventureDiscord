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

            if (id.StartsWith("move_"))// || id.StartsWith("blocked_"))
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
                LogService.Info($"[WalkDirectionHandler] Received data: {data}");
                await Context.Interaction.DeferAsync();

                var parts = data.Split(':');
                if (parts.Length < 3)
                {
                    await Context.Interaction.FollowupAsync("⚠️ Invalid move data.", ephemeral: true);
                    return;
                }

                string areaId = parts[1];
                string tileId = parts[2];
                string key = $"{areaId}:{tileId}";

                await ComponentHelpers.MovePlayerAsync(Context, key);
            }
            catch (Exception ex)
            {
                LogService.Error($"[WalkDirectionHandler] Exception:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while moving.");
            }
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
            try
            {
                LogService.Info($"[EnterTileHandler] Received data: {data}");
                await Context.Interaction.DeferAsync();

                var parts = data.Split(':');

                string areaName = parts[1];
                string tileId = parts[2];
                string key = $"{areaName}:{tileId}";

                await ComponentHelpers.MovePlayerAsync(Context, key, showTravelAnimation: true);
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