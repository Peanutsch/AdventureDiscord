using Discord.Interactions;
using Discord;
using Adventure.Data;
using Adventure.Services;
using Adventure.Quest.Encounter;
using Discord.WebSocket;
using Adventure.Quest.Battle.BattleEngine;

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
            await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "flee", "none");
        }

        [ComponentInteraction("battle_continue_*")]
        public async Task ContinueBattleHandler(string userIdRaw)
        {
            LogService.Info($"[ContinueBattleHandler] User {Context.User.Username} wants to continue.");

            if (Context.User.Id.ToString() != userIdRaw)
            {
                await RespondAsync("⚠️ You can't interact with this battle!", ephemeral: true);
                return;
            }

            // Zet de battle-stap terug naar de wapenkeuze
            EncounterBattleStepsSetup.SetStep(Context.User.Id, EncounterBattleStepsSetup.StepWeaponChoice);

            // Toon opnieuw de wapenkeuze
            await EmbedBuilders.EmbedPreBattle((SocketMessageComponent)Context.Interaction);
        }

        [ComponentInteraction("battle_flee_*")]
        public async Task FleeBattleHandler(string userIdRaw)
        {
            LogService.Info($"[FleeBattleHandler] User {Context.User.Username} tries to flee.");

            if (Context.User.Id.ToString() != userIdRaw)
            {
                await RespondAsync("⚠️ You can't interact with this battle!", ephemeral: true);
                return;
            }

            await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "flee", "none");
        }

    }
}
