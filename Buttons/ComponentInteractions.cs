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
        #region === Battle ===
        [ComponentInteraction("*")]
        public async Task DispatchComponentAction(string weaponId)
        {
            LogService.Info($"[DispatchComponentAction] component ID: {weaponId}");

            if (weaponId.StartsWith("weapon_"))
            {
                await HandleWeaponButton(weaponId);
                return;
            }

            //await FollowupAsync($"You clicked: {weaponId}\nNo ComponentInteraction match found...");
        }

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
            var parts = data.Split(':');
            string direction = parts[0];
            string targetTileId = parts[1];

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
        #endregion
    }
}