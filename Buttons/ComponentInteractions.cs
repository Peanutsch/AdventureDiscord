// ComponentInteractions.cs
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
        [ComponentInteraction("*")]
        public async Task DispatchComponentAction(string id)
        {
            LogService.Info($"[ComponentInteractions.DispatchComponentAction] component ID: {id}");

            if (id.StartsWith("weapon_"))
                await HandleWeaponButton(id);
            else if (id.StartsWith("move:"))
                await WalkDirectionHandler(id);
            else if (id.StartsWith("enter:"))
                await EnterTileHandler(id);
        }
        #endregion

        #region === Battle ===
        [ComponentInteraction("weapon_*")]
        public async Task HandleWeaponButton(string weaponId)
        {
            LogService.Info($"[HandleWeaponButton] weaponId: {weaponId}");

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
                await RespondAsync($"⚠️ Weapon not found: {weaponId}");
                return;
            }

            await EncounterBattleStepsSetup.HandleStepBattle(Context.Interaction, weaponId);
        }

        [ComponentInteraction("btn_attack")]
        public async Task ButtonAttackHandler()
            => await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "attack", "none");

        [ComponentInteraction("btn_flee")]
        public async Task ButtonFleeHandler()
        {
            try
            {
                await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "flee", "none");
            }
            catch
            {
                await Context.Interaction.FollowupAsync(embed: new EmbedBuilder().WithDescription("You tried to flee...").Build());
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
            catch
            {
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
            catch
            {
                await Context.Interaction.FollowupAsync(embed: new EmbedBuilder().WithDescription("You tried to flee...").Build());
            }
        }
        #endregion

        #region === Walk Direction Handler ===
        //[ComponentInteraction("move:*")]
        public async Task WalkDirectionHandler(string data)
        {
            await Context.Interaction.DeferAsync();
            try
            {
                string tileId = data.Substring("move:".Length);

                if (!TestHouseLoader.TileLookup.ContainsKey(tileId))
                {
                    LogService.Error($"[ComponentInteractions.WalkDirectionHandler] ❌ Tile '{tileId}' not found");
                    await Context.Interaction.FollowupAsync($"❌ Tile '{tileId}' not found!", ephemeral: true);
                    return;
                }

                await ComponentHelpers.MovePlayerAsync(Context, tileId);
            }
            catch (Exception ex)
            {
                LogService.Error($"[ComponentInteractions.WalkDirectionHandler] Error while moving:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while moving.");
            }
        }
        #endregion

        #region === Enter Button Handler ===
        public async Task EnterTileHandler(string data)
        {
            await Context.Interaction.DeferAsync();
            try
            {
                string tileId = data.Substring("enter:".Length);

                if (!TestHouseLoader.TileLookup.TryGetValue(tileId, out var targetTile))
                {
                    LogService.Error($"[ComponentInteractions.EnterTileHandler] ❌ Tile '{tileId}' not found");
                    await Context.Interaction.FollowupAsync($"❌ Target tile '{tileId}' not found.", ephemeral: false);
                    return;
                }

                LogService.Info($"[ComponentInteractions.EnterTileHandler] TileType: {targetTile.TileType}");

                await ComponentHelpers.MovePlayerAsync(Context, tileId, showTravelAnimation: true);
            }
            catch (Exception ex)
            {
                LogService.Error($"[ComponentInteractions.EnterTileHandler] Error:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while entering the new area.");
            }
        }
        #endregion
    }
}
