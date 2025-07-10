// Adventure/Quest/QuestEngine.cs
using System.Collections.Concurrent;
using System.Linq;
using Adventure.Buttons;
using Adventure.Data;
using Adventure.Helpers;
using Adventure.Models.Items;
using Adventure.Models.Player;
using Adventure.Services;
using Discord;
using Discord.WebSocket;

namespace Adventure.Quest
{
    public static class QuestEngine
    {
        private const string StepStart = "start";
        private const string StepFlee = "flee";
        private const string StepWeaponChoice = "weapon_choice";
        private const string StepPostBattle = "post_battle";

        private const string ActionFlee = "flee";
        private const string ActionAttack = "attack";

        private const string MsgFlee = "You fled. The forest grows quiet.";
        private const string MsgChooseWeapon = "Choose your weapon:";
        private const string MsgAttackShortsword = "You attack with your **Shortsword**!";
        private const string MsgAttackDagger = "You attack with your **Dagger**!";
        private const string MsgUnknownWeapon = "You attack with your weapon!";
        private const string MsgNothingHappens = "Nothing happens...";

        private static readonly ConcurrentDictionary<ulong, string> playerSteps = new();

        public static string GetStep(ulong userId) =>
            playerSteps.TryGetValue(userId, out var step) ? step : StepStart;

        public static void SetStep(ulong userId, string step) =>
            playerSteps[userId] = step;

        public static async Task HandleEncounterAction(SocketInteraction interaction, string action)
        {
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);
            var inventory = GameStateService.GetState(userId).Inventory;

            switch (currentStep)
            {
                case StepStart:
                    if (action == ActionFlee)
                    {
                        LogService.Info("[QuestEngine.HandleEncounterAction] > [action == ActionFlee] > Button Action FLEE");

                        if (interaction is SocketMessageComponent componentFlee)
                        {
                            LogService.Info("With ComponentFlee");
                            await ButtonInteractionHelpers.RemoveButtonsAsync(componentFlee, MsgFlee);
                        }
                        else
                        {
                            LogService.Error("No componentFlee");
                            await interaction.RespondAsync(MsgFlee, ephemeral: false);
                        }

                        SetStep(userId, StepFlee);
                    }
                    else if (action == ActionAttack)
                    {
                        LogService.Info($"[QuestEngine.HandleEncounterAction] > [action == ActionAttack] > Inventory count: {inventory.Count}");

                        if (interaction is SocketMessageComponent componentAttack)
                        {
                            var builder = new ComponentBuilder();
                            var weaponIds = inventory.Keys.Select(k => k.ToLower()).ToList();
                            var weapons = GameEntityFetcher.RetrieveWeaponAttributes(weaponIds);

                            foreach (var weapon in weapons)
                            {
                                string customId = $"btn_{weapon.Id}";
                                string label = $"Attack with {weapon.Name}";

                                LogService.Info($"[QuestEngine.HandleEncounterAction] > [action == ActionAttack] > Label: {label}, customId: {customId}");

                                builder.WithButton(label, customId, ButtonStyle.Primary);
                            }

                            await componentAttack.UpdateAsync(msg =>
                            {
                                var embed = componentAttack.Message.Embeds.FirstOrDefault()?.ToEmbedBuilder()?.Build();
                                msg.Embeds = embed != null ? new[] { embed } : null;
                                msg.Content = "";
                                msg.Components = builder.Build();
                            });

                            SetStep(userId, StepWeaponChoice);
                        }
                        else
                        {
                            await interaction.RespondAsync(MsgChooseWeapon, ephemeral: false);
                        }
                    }
                    break;

                case StepWeaponChoice:
                {
                    WeaponModel? weapon = GetWeaponFromActionId(action);
                    LogService.Info($"[QuestEngine.HandleEncounterAction] > [Case StepWeaponChoice] > Chosen weapon: {weapon?.Name}");

                    if (weapon == null)
                    {
                        await interaction.RespondAsync("You selected an unknown weapon...", ephemeral: false);
                        return;
                    }

                    if (inventory.ContainsKey(weapon.Name!.ToLower()))
                    {
                        string weaponUsed = weapon.Name.ToLower() switch
                        {
                            "shortsword" => MsgAttackShortsword,
                            "dagger" => MsgAttackDagger,
                            _ => $"You attack with your {weapon.Name}!"
                        };

                        if (interaction is SocketMessageComponent componentWeaponChoice)
                        {
                            await ButtonInteractionHelpers.RemoveButtonsAsync(componentWeaponChoice, weaponUsed);
                        }
                        else
                        {
                            await interaction.RespondAsync(weaponUsed, ephemeral: false);
                        }

                        SetStep(userId, StepPostBattle);
                    }
                    else
                    {
                        await interaction.RespondAsync($"You fumble with the unfamiliar {weapon.Name}...", ephemeral: false);
                    }

                    break;
                }

                default:
                    await interaction.RespondAsync(MsgNothingHappens, ephemeral: false);
                    break;
            }
        }

        private static WeaponModel? GetWeaponFromActionId(string action)
        {
            LogService.Info($"[QuestEngine.GetWeaponFromActionId] > param action: {action}");

            if (string.IsNullOrEmpty(action) || GameData.Weapons == null)
                return null;

            if (action.StartsWith("btn_"))
            {
                var weaponId = action.Substring(4); // Strip 'btn_'
                return GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();
            }

            return null;
        }
    }
}
