using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Adventure.Buttons;
using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Encounter;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Handles the step-by-step flow of an encounter battle.
    /// Responsibilities include:
    /// - Tracking active battle states per player
    /// - Processing player and NPC actions
    /// - Handling weapon selection
    /// - Executing attacks
    /// - Ending the battle when one side is defeated
    /// </summary>
    public static class EncounterBattleStepsSetup
    {
        #region === Constants ===

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

        #endregion

        #region === Battle State Tracking ===

        /// <summary>
        /// Tracks active battle states for each user by Discord user ID.
        /// </summary>
        public static readonly ConcurrentDictionary<ulong, BattleState> battleStates = new();

        /// <summary>
        /// Gets the current step of the user's battle.
        /// Defaults to StepStart if no step is set.
        /// </summary>
        public static string GetStep(ulong userId) =>
            BattleStateSetup.GetBattleState(userId).Player.Step ?? StepStart;

        /// <summary>
        /// Sets the current step for a user's battle.
        /// Updates both the local state and the dictionary.
        /// </summary>
        public static void SetStep(ulong userId, string step)
        {
            var state = BattleStateSetup.GetBattleState(userId);
            state.Player.Step = step;
            battleStates[userId] = state;
        }

        #endregion

        #region === Main Dispatcher ===

        /// <summary>
        /// Main dispatcher for handling player actions during a battle.
        /// Routes execution to the correct step handler based on the current step.
        /// </summary>
        /// <param name="interaction">The Discord interaction object (button or slash command).</param>
        /// <param name="action">The player's chosen action (attack or flee).</param>
        /// <param name="weaponId">The weapon ID selected by the player (if any).</param>
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
                    // Battle is processed separately via button interactions
                    break;

                case StepPostBattle:
                    await HandleStepPostBattle(interaction);
                    break;

                case StepEndBattle:
                    await EmbedBuilders.EmbedEndBattle(interaction);
                    break;

                default:
                    await interaction.RespondAsync(MsgNothingHappens, ephemeral: false);
                    break;
            }
        }

        #endregion

        #region === Step: Start ===

        /// <summary>
        /// Handles the initial step where the player chooses to attack or flee.
        /// </summary>
        /// <param name="component">The button component interaction triggered by the player.</param>
        /// <param name="action">The action chosen by the player (attack or flee).</param>
        public static async Task HandleStepStart(SocketMessageComponent component, string action)
        {
            ulong userId = component.User.Id;
            LogService.DividerParts(1, "HandleStepStart");

            if (action == ActionFlee)
            {
                // Player chose to flee → remove buttons and show message
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
                // Player chose attack → show weapon selection
                LogService.Info("[HandleStepStart] Player chooses attack, showing weapons...");
                await EmbedBuilders.EmbedPreBattle(component);
                SetStep(userId, StepWeaponChoice);
            }

            LogService.DividerParts(2, "HandleStepStart");
        }

        #endregion

        #region === Step: Weapon Choice ===

        /// <summary>
        /// Handles the weapon selection step.
        /// Validates if the player owns the selected weapon and proceeds to the battle step.
        /// </summary>
        private static async Task HandleStepWeaponChoice(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);
            var ownedWeaponIds = state.Player.Weapons.Select(w => w.Id).ToHashSet();

            var weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();
            if (weapon == null)
            {
                await interaction.RespondAsync("Je hebt een onbekend wapen gekozen...", ephemeral: true);
                return;
            }

            if (!ownedWeaponIds.Contains(weaponId))
            {
                await interaction.RespondAsync($"Je rommelt met een onbekend wapen: {weapon.Name}...", ephemeral: true);
            }

            // Zet stap naar battle
            SetStep(userId, StepBattle);

            // Start battle
            await HandleStepBattle(interaction, weaponId);
        }

        #endregion

        #region === Step: Battle ===

        /// <summary>
        /// Executes both the player and NPC attack sequences during battle.
        /// Handles all turn-based actions, builds the updated battle embed and buttons,
        /// and transitions to the post-battle step if the battle is still ongoing.
        /// </summary>
        /// <param name="interaction">The Discord interaction that triggered the battle step (e.g. a weapon button).</param>
        /// <param name="weaponId">The identifier of the weapon chosen by the player.</param>
        public static async Task HandleStepBattle(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);

            // --- Validate weapon existence ---
            var weapon = state.PlayerWeapons.FirstOrDefault(w => w.Id == weaponId);
            if (weapon == null)
            {
                await interaction.FollowupAsync("⚠️ Weapon not found in your inventory.", ephemeral: true);
                return;
            }

            // --- Player attack phase ---
            string playerAttackResult = PlayerAttack.ProcessPlayerAttack(userId, weapon);

            // --- Check if NPC is defeated after player's attack ---
            if (state.CurrentHitpointsNPC <= 0 || state.StateOfNPC == "Defeated")
            {
                // End the battle and send victory embed
                await EmbedBuilders.EmbedEndBattle(interaction, playerAttackResult);
                return;
            }

            // --- NPC attack phase ---
            string npcAttackResult = state.NpcWeapons.FirstOrDefault() is { } npcWeapon
                ? NpcAttack.ProcessNpcAttack(userId, npcWeapon)
                : $"⚠️ {state.Npc.Name} has nothing to attack with.";

            // Combine both attack summaries
            string fullAttackLog = $"{playerAttackResult}\n\n{npcAttackResult}";

            // --- Build embed and battle buttons ---
            var battleEmbed = EmbedBuilders.BuildBattleEmbed(userId, fullAttackLog);
            var battleButtons = EmbedBuilders.BuildBattleButtons(state);

            try
            {
                // Update the existing Discord message if interaction is a component (button press)
                if (interaction is SocketMessageComponent component)
                {
                    await component.UpdateAsync(msg =>
                    {
                        msg.Embed = battleEmbed.Build();
                        msg.Components = battleButtons.Build();
                        msg.Content = string.Empty;
                    });
                }
                else
                {
                    // Fallback: handle as a follow-up if the interaction was not from a component
                    await interaction.FollowupAsync(embed: battleEmbed.Build(), components: battleButtons.Build());
                }
            }
            catch (Exception ex)
            {
                // If UpdateAsync fails (e.g. expired token or already acknowledged interaction)
                LogService.Info($"[HandleStepBattle] UpdateAsync failed, fallback to FollowupAsync. {ex.Message}");
                await interaction.FollowupAsync(embed: battleEmbed.Build(), components: battleButtons.Build());
            }

            // --- Transition to post-battle step ---
            SetStep(userId, StepPostBattle);
            await HandleStepPostBattle(interaction);
        }
        #endregion

        #region === Step: Post Battle ===

        /// <summary>
        /// Handles post-battle logic.
        /// Ends the battle if the player or NPC is defeated; otherwise, returns to weapon selection.
        /// </summary>
        public static async Task HandleStepPostBattle(SocketInteraction interaction)
        {
            if (interaction == null) return;

            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);
            if (state == null)
            {
                await interaction.FollowupAsync("No battle found...", ephemeral: true);
                return;
            }

            // End battle if player or NPC is dead
            if (state.Player.Hitpoints <= 0 || state.StateOfNPC == "Defeated")
            {
                SetStep(userId, StepEndBattle);
                await EmbedBuilders.EmbedEndBattle(interaction);
                return;
            }

            // Otherwise, return to weapon choice step
            SetStep(userId, StepWeaponChoice);
        }

        #endregion
    }
}