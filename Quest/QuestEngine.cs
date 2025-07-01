// Adventure/Quest/QuestEngine.cs
using System.Collections.Concurrent;
using Adventure.Buttons;
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
                        if (interaction is SocketMessageComponent componentFlee)
                        {
                            await ButtonInteractionHelpers.RemoveButtonsAsync(
                                componentFlee,
                                "You fled. The forest grows quiet."
                            );
                        }
                        else
                        {
                            await interaction.RespondAsync("You fled. The forest grows quiet.", ephemeral: false);
                        }

                        SetStep(userId, "flee");
                    }
                    else if (action == "attack")
                    {
                        if (interaction is SocketMessageComponent componentAttack)
                        {
                            var weaponButtons = new ComponentBuilder()
                                .WithButton("Attack with Shortsword", "btn_weapon_shortsword", ButtonStyle.Primary)
                                .WithButton("Attack with Dagger", "btn_weapon_dagger", ButtonStyle.Primary);

                            await componentAttack.UpdateAsync(msg =>
                            {
                                var embed = componentAttack.Message.Embeds.FirstOrDefault()?.ToEmbedBuilder()?.Build();
                                msg.Embeds = embed != null ? new[] { embed } : null;
                                msg.Content = ""; // geen content erboven
                                msg.Components = weaponButtons.Build();
                            });

                            SetStep(userId, "weapon_choice");
                        }
                        else
                        {
                            await interaction.RespondAsync("Choose your weapon:", ephemeral: false);
                        }
                    }
                    break;

                case "weapon_choice":
                    if (interaction is SocketMessageComponent componentWeaponChoice)
                    {
                        string weaponUsed = action switch
                        {
                            "Shortsword" => "Shortsword",
                            "Dagger" => "Dagger",
                            _ => $"Unknown weapon ({action})"
                        };

                        await ButtonInteractionHelpers.RemoveButtonsAsync(
                            componentWeaponChoice,
                            $"You attack with your [**{weaponUsed}**]!"
                        );
                    }
                    else
                    {
                        await interaction.RespondAsync($"You attack with your weapon!", ephemeral: false);
                    }

                    SetStep(userId, "post_battle");
                    break;

                default:
                    await interaction.RespondAsync("Nothing happens...", ephemeral: false);
                    break;
            }
        }
    }
}
