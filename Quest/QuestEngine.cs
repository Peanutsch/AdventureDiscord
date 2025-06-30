using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;

namespace Adventure.Quest
{
    public static class QuestEngine
    {
        private static readonly ConcurrentDictionary<ulong, string> playerSteps = new();

        public static string GetStep(ulong userId) =>
            playerSteps.TryGetValue(userId, out var step) ? step : "start";

        public static void SetStep(ulong userId, string step) =>
            playerSteps[userId] = step;

        public static async Task HandleEncounterAction(SocketInteraction interaction, string action)
        {
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);

            switch (currentStep)
            {
                case "start":
                    if (action == "flee")
                    {
                        await interaction.RespondAsync("You fled. The forest grows quiet.", ephemeral: false);
                        SetStep(userId, "fled_goblins");
                    }
                    else if (action == "attack")
                    {
                        var weaponButtons = new ComponentBuilder()
                            .WithButton("Shortsword", "btn_weapon_shortsword", ButtonStyle.Primary)
                            .WithButton("Dagger", "btn_weapon_dagger", ButtonStyle.Primary);

                        await interaction.RespondAsync("Choose your weapon:", components: weaponButtons.Build(), ephemeral: false);
                        SetStep(userId, "weapon_choice");
                    }
                    break;

                case "weapon_choice":
                    await interaction.RespondAsync($"You attack with your {action}!", ephemeral: false);
                    SetStep(userId, "post_battle");
                    break;
                default:
                    await interaction.RespondAsync("Nothing happens...", ephemeral: false);
                    break;
            }
        }
    }
}
