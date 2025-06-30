using Discord.Interactions;
using Discord;
using Adventure.Quest;

namespace Adventure.Buttons.Handlers
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
    }
}
