using System.Collections.Concurrent;
using System.Linq;
using Adventure.Buttons;
using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Encounter;
using Adventure.Quest.Helpers;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Handles the step-by-step flow of an encounter battle.
    /// Tracks battle state, processes player/NPC actions, weapon selection,
    /// attacks, post-battle handling, and battle completion.
    /// </summary>
    public static class EncounterBattleStepsSetup
    {
        // Battle step constants
        public const string StepStart = "start";
        public const string StepFlee = "flee";
        public const string StepWeaponChoice = "weapon_choice";
        public const string StepBattle = "fight";
        public const string StepPostBattle = "post_battle";
        public const string StepEndBattle = "end_battle";

        // Player actions
        public const string ActionFlee = "flee";
        public const string ActionAttack = "attack";

        // Predefined messages
        public const string MsgFlee = "You fled. The forest grows quiet.";
        public const string MsgChooseWeapon = "Choose your weapon:";
        public const string MsgBattleOver = "Battle is over!";
        public const string MsgNothingHappens = "Nothing happens...";

        // Tracks active battle states per user
        public static readonly ConcurrentDictionary<ulong, BattleStateModel> battleStates = new();

        /// <summary>
        /// Gets the current step of the user's battle.
        /// </summary>
        public static string GetStep(ulong userId) =>
            BattleStateSetup.GetBattleState(userId).Player.Step ?? StepStart;

        /// <summary>
        /// Sets the current step for a user's battle.
        /// </summary>
        public static void SetStep(ulong userId, string step)
        {
            var state = BattleStateSetup.GetBattleState(userId);
            state.Player.Step = step;
            battleStates[userId] = state;
        }

        /// <summary>
        /// Main handler for user actions during the battle.
        /// Routes to appropriate step handler based on current step.
        /// </summary>
        public static async Task HandleEncounterAction(SocketInteraction interaction, string action, string weaponId)
        {
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);

            LogService.Info($">>> [Current step: {currentStep}, action: {action}, weaponId: {weaponId}] <<<\n");

            switch (currentStep)
            {
                case StepStart:
                    await HandleStepStart((SocketMessageComponent)interaction, action);
                    break;

                case StepWeaponChoice:
                    await HandleStepWeaponChoice(interaction, weaponId);
                    break;

                case StepBattle:
                    // Battle handled externally via button interactions
                    break;

                case StepPostBattle:
                    await HandleStepPostBattle(interaction);
                    break;

                case StepEndBattle:
                    await EndBattle(interaction);
                    break;

                default:
                    await interaction.RespondAsync(MsgNothingHappens, ephemeral: false);
                    break;
            }
        }

        /// <summary>
        /// Handles the first step where the player chooses to attack or flee.
        /// </summary>
        public static async Task HandleStepStart(SocketMessageComponent component, string action)
        {
            ulong userId = component.User.Id;
            LogService.DividerParts(1, "HandleStepStart");

            if (action == ActionFlee)
            {
                LogService.Info("[HandleStepStart] Player flees");
                await component.UpdateAsync(msg =>
                {
                    msg.Content = MsgFlee;
                    msg.Components = new ComponentBuilder().Build(); // remove buttons
                    msg.Embed = null;
                });
                SetStep(userId, StepFlee);
            }
            else if (action == ActionAttack)
            {
                LogService.Info("[HandleStepStart] Player chooses attack, showing weapons...");
                await EncounterService.PrepareForBattleChoices(component);
                SetStep(userId, StepWeaponChoice);
            }

            LogService.DividerParts(2, "HandleStepStart");
        }

        /// <summary>
        /// Handles weapon selection and validates inventory.
        /// Moves to battle step after selection.
        /// </summary>
        private static async Task HandleStepWeaponChoice(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);
            var ownedWeaponIds = state.Player.Weapons.Select(w => w.Id).ToHashSet();

            LogService.DividerParts(1, "HandleStepWeaponChoice");

            var weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();

            if (weapon == null)
            {
                await interaction.RespondAsync("You selected an unknown weapon...", ephemeral: false);
                LogService.Error("[HandleStepWeaponChoice] Invalid weapon selected.");
                return;
            }

            if (ownedWeaponIds.Contains(weaponId))
            {
                string message = $"You attack with your {weapon.Name}!";
                if (interaction is SocketMessageComponent componentWeaponChoice)
                {
                    SetStep(userId, StepBattle);
                    await ButtonInteractionHelpers.RemoveButtonsAsync(componentWeaponChoice, message);
                }
                else
                {
                    SetStep(userId, StepBattle);
                    await interaction.RespondAsync(message, ephemeral: false);
                }
            }
            else
            {
                SetStep(userId, StepBattle);
                await interaction.RespondAsync($"You fumble with the unfamiliar {weapon.Name}...", ephemeral: false);
            }

            LogService.DividerParts(2, "HandleStepWeaponChoice");
        }

        /// <summary>
        /// Executes player and NPC attacks and updates the battle state.
        /// </summary>
        public static async Task HandleStepBattle(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);

            LogService.DividerParts(1, "HandleStepBattle");

            await interaction.DeferAsync(); // Acknowledge interaction

            var weapon = state.PlayerWeapons.FirstOrDefault(w => w.Id == weaponId);
            if (weapon == null)
            {
                await interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"⚠️ Weapon not found in your inventory.");
                return;
            }

            // ⚔️ Player attacks
            string playerAttackResult = PlayerAttack.ProcessPlayerAttack(userId, weapon);

            // Check if NPC died
            if (state.StateOfNPC == "Dead")
            {
                await EndBattle(interaction, playerAttackResult);
                return;
            }

            // 💥 NPC attacks only if alive
            string npcAttackResult = "";
            var npcWeapon = state.NpcWeapons.FirstOrDefault();
            if (npcWeapon != null)
            {
                npcAttackResult = NpcAttack.ProcessNpcAttack(userId, npcWeapon);
            }
            else
            {
                npcAttackResult = $"⚠️ {state.Npc.Name} has nothing to attack with.";
            }

            // 📦 Combine attack logs
            string fullAttackLog = $"{playerAttackResult}\n\n{npcAttackResult}";

            // 🧱 Rebuild embed and update message
            var fullEmbed = EncounterService.RebuildBattleEmbed(userId, fullAttackLog);
            await interaction.ModifyOriginalResponseAsync(msg => msg.Embed = fullEmbed.Build());

            // Move to post-battle step
            SetStep(userId, StepPostBattle);
            await HandleStepPostBattle(interaction);

            LogService.DividerParts(2, "HandleStepBattle");
        }

        /// <summary>
        /// Handles post-battle state updates and moves to the next step.
        /// Ends the battle if either player or NPC is dead.
        /// </summary>
        public static async Task HandleStepPostBattle(SocketInteraction interaction)
        {
            if (interaction == null)
                return;

            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);

            if (state == null)
            {
                await interaction.RespondAsync("No battle found...");
                return;
            }

            // End battle if player or NPC is dead
            if (state.Player.Hitpoints <= 0 || state.StateOfNPC == "Dead")
            {
                SetStep(userId, StepEndBattle);
                await EndBattle(interaction);
                return;
            }

            // Otherwise, continue battle -> next weapon selection
            SetStep(userId, StepWeaponChoice);
        }

        /// <summary>
        /// Ends the battle: removes buttons and displays the battle-over message.
        /// </summary>
        /// <summary>
        /// Ends the battle, removes buttons, and updates the message with the battle log and MsgBattleOver.
        /// </summary>
        public static async Task EndBattle(SocketInteraction interaction, string? extraMessage = null)
        {
            // Build final message content
            string content = extraMessage != null ? $"{extraMessage}\n\n{EncounterBattleStepsSetup.MsgBattleOver}"
                                                  : EncounterBattleStepsSetup.MsgBattleOver;

            // Modify the original deferred response
            await interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = content;
                msg.Components = new ComponentBuilder().Build(); // remove buttons
                msg.Embed = null; // remove any embed if needed
            });

            // Update battle step to end
            ulong userId = interaction.User.Id;
            EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepEndBattle);
        }
    }
}
