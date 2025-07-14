using Discord.Interactions;
using Discord;
using Adventure.Data;
using Adventure.Events.EventService;
using Adventure.Services;
using Adventure.Helpers;
using Adventure.Quest.Battle;

namespace Adventure.Buttons
{
    public class ComponentInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("*")]
        public async Task CatchAll(string id)
        {
            LogService.Info($"[CatchAll] component ID: {id}");
            await RespondAsync($"You clicked: {id}\nNo ComponentInteraction match found...", ephemeral: false);
        }

        [ComponentInteraction("_*")]
        public async Task HandleDynamicWeaponButton(string weaponId)
        {
            LogService.Info($"[HandleDynamicWeaponButton] > Param customId: {weaponId}");

            var state = BattleEngine.GetBattleState(Context.User.Id);
            if (state == null)
            {
                await RespondAsync("❌ No active battle found.", ephemeral: true);
                return;
            }

            var weapon = GameEntityFetcher
                .RetrieveWeaponAttributes(new List<string> { weaponId })
                .FirstOrDefault();

            if (weapon == null)
            {
                LogService.Error($"[HandleDynamicWeaponButton] > Weapon ID '{weaponId}' not found.");
                await RespondAsync($"⚠️ Weapon not found: {weaponId}", ephemeral: true);
                return;
            }

            LogService.Info($"[HandleDynamicWeaponButton] > Player chose: {weapon.Name}");

            await BattleEngine.HandleEncounterAction(Context.Interaction, weapon.Name!, weaponId);
        }

        [ComponentInteraction("btn_attack")]
        public async Task ButtonAttackHandler()
        {
            await BattleEngine.HandleEncounterAction(Context.Interaction, "attack", "none");
        }

        [ComponentInteraction("btn_flee")]
        public async Task ButtonFleeHandler()
        {
            await BattleEngine.HandleEncounterAction(Context.Interaction, "flee", "none");
        }
    }
}
