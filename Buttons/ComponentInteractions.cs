using Discord.Interactions;
using Discord;
using Adventure.Quest;
using Adventure.Data;
using Adventure.Events.EventService;

namespace Adventure.Buttons
{
    public class ComponentInteractions : InteractionModuleBase<SocketInteractionContext>
    {
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

        [ComponentInteraction("btn_weapon_shortsword")]
        public async Task WeaponShortswordHandler()
        {
            await QuestEngine.HandleEncounterAction(Context.Interaction, "Shortsword");
        }

        [ComponentInteraction("btn_weapon_dagger")]
        public async Task WeaponDaggerHandler()
        {
            await QuestEngine.HandleEncounterAction(Context.Interaction, "Dagger");
        }

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
