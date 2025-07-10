using Discord.Interactions;
using Discord;
using Adventure.Quest;
using Adventure.Data;
using Adventure.Events.EventService;
using Adventure.Services;
using Adventure.Helpers;

namespace Adventure.Buttons
{
    public class ComponentInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        /*
        [ComponentInteraction("*")]
        public async Task CatchAll(string id)
        {
            LogService.Info($"[CatchAll] component ID: {id}");
            await RespondAsync($"You clicked: {id}", ephemeral: true);
        }
        */

        [ComponentInteraction("btn_:*")]
        public async Task HandleDynamicWeaponButton(string customId)
        {
            var preparedWeaponId = $"{customId.Replace("btn_", "")}";
            var weapons = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> {preparedWeaponId});
            var weapon = weapons.FirstOrDefault();

            LogService.Info($"[ComponentInteractions.HandleDynamicWeaponButton] > Param customId: {customId}");
            LogService.Info($"[ComponentInteractions.HandleDynamicWeaponButton] > preparedWeaponId: {preparedWeaponId}");

            if (weapon == null)
            {
                LogService.Error($"[ComponentInteractions.HandleDynamicWeaponButton] > Weapon ID '{preparedWeaponId}' not found.");
                await RespondAsync($"⚠️ Weapon not found: {preparedWeaponId}", ephemeral: true);
                return;
            }

            LogService.Info($"[ComponentInteractions.HandleDynamicWeaponButton] > weaponName: {weapon!.Name}");

            await QuestEngine.HandleEncounterAction(Context.Interaction, weapon.Name!);
        }

        [ComponentInteraction("btn_attack")]
        public async Task ButtonAttackHandler()
        {
            await QuestEngine.HandleEncounterAction(Context.Interaction, "attack");
        }

        [ComponentInteraction("btn_flee")]
        public async Task ButtonFleeHandler()
        {
            await QuestEngine.HandleEncounterAction(Context.Interaction, "flee");
        }

        /*
        [ComponentInteraction("btn_weapon_short_sword")]
        public async Task WeaponShortswordHandler()
        {
            await QuestEngine.HandleEncounterAction(Context.Interaction, "Shortsword");
        }

        [ComponentInteraction("btn_weapon_dagger")]
        public async Task WeaponDaggerHandler()
        {
            await QuestEngine.HandleEncounterAction(Context.Interaction, "Dagger");
        }
        */

        [ComponentInteraction("btn_toggle_detail:*")]
        public async Task HandleToggleDetail(string creatureId)
        {
            await DeferAsync();

            var creature = GameData.Humanoids!.FirstOrDefault(c => c.Id == creatureId);
            if (creature == null)
            {
                await FollowupAsync("⚠️ Could not find this creature anymore.");
                return;
            }

            var embed = EncounterService.GetRandomEncounter(creature);

            var updatedButtons = new ComponentBuilder()
                .WithButton("Attack", "btn_attack", ButtonStyle.Danger)
                .WithButton("Flee", "btn_flee", ButtonStyle.Secondary);

            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embed.Build();
                msg.Components = updatedButtons.Build();
            });
        }

    }
}
