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
        public async Task DispatchComponentAction(string weaponId)
        {
            LogService.Info($"[DispatchComponentAction] component ID: {weaponId}");

            if (weaponId.StartsWith("weapon_"))
            {
                await HandleWeaponButton(weaponId);
            }
                
            await RespondAsync($"You clicked: {weaponId}\nNo ComponentInteraction match found...");
        }

        public async Task HandleWeaponButton(string weaponId)
        {
            LogService.Info($"[ComponentInteractions.HandleWeaponButton] > Recieved weaponId: {weaponId}");

            ulong userId = Context.User.Id;

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
                LogService.Error($"[ComponentInteractions.HandleWeaponButton] > Weapon ID '{weaponId}' not found.");
                await RespondAsync($"⚠️ Weapon not found: {weaponId}");
                return;
            }

            LogService.Info($"[ComponentInteractions.HandleWeaponButton] > Player choose: {weapon.Name}\n");

            var step = BattleEngine.GetStep(userId);
            LogService.Info($"[HandleWeaponButton] Current battle step: {step}");

            //await BattleEngine.HandleEncounterAction(Context.Interaction, "fight", weaponId);
            // Direct call BattleEngine.HandleStepBattle
            await BattleEngine.HandleStepBattle(Context.Interaction, weaponId);
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
