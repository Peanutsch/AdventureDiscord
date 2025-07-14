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
        public async Task CatchAll(string weaponId)
        {
            LogService.Info($"[CatchAll] component ID: {weaponId}");

            if (weaponId.StartsWith("weapon_"))
            {
                await HandleWeaponButton(weaponId);
            }
                
            await RespondAsync($"You clicked: {weaponId}\nNo ComponentInteraction match found...");
        }

        //[ComponentInteraction("_*")]
        public async Task HandleWeaponButton(string weaponId)
        {
            LogService.Info($"[HandleWeaponButton] > Recieved weaponId: {weaponId}");

            var state = BattleEngine.GetBattleState(Context.User.Id);
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
                LogService.Error($"[HandleWeaponButton] > Weapon ID '{weaponId}' not found.");
                await RespondAsync($"⚠️ Weapon not found: {weaponId}");
                return;
            }

            LogService.Info($"[HandleWeaponButton] > Player chose: {weapon.Name}");

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
