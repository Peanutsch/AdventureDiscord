using Adventure.Models.BattleState;
using Adventure.Quest.Encounter;
using Adventure.Services;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Central orchestrator for multi-step encounter battle flow management.
    /// 
    /// This static class handles the state machine logic for battles, tracking:
    /// - Current battle phase (start, weapon selection, combat, post-battle, end)
    /// - Active battles per player using concurrent dictionary
    /// - Routing player actions to appropriate handlers
    /// - Managing turn sequencing and battle transitions
    /// 
    /// Battle Phases:
    /// 1. START - Initial battle setup, player weapon selection prompt
    /// 2. WEAPON_CHOICE - Player selects their equipment
    /// 3. BATTLE (FIGHT) - Combat resolution loop
    /// 4. POST_BATTLE - Victory/defeat aftermath
    /// 5. END_BATTLE - Clean up and exit
    /// 
    /// This class works in conjunction with BattleStateSetup and specific action handlers
    /// to provide a complete battle experience.
    /// 
    /// <remarks>
    /// Thread Safety: Uses ConcurrentDictionary for thread-safe state tracking.
    /// 
    /// Usage Pattern:
    /// 1. SetStep(userId, "start") - Initialize battle
    /// 2. HandleEncounterAction() - Route player actions
    /// 3. GetStep(userId) - Check current phase
    /// 4. Clean up when step == "end_battle"
    /// </remarks>
    /// </summary>
    public static class EncounterBattleStepsSetup
    {
        #region === Constants ===
        // Battle step constants - represent the current phase of combat
        public const string StepStart = "start";                    // Initial setup, awaiting weapon selection
        public const string StepFlee = "flee";                      // Fleeing from battle
        public const string StepWeaponChoice = "weapon_choice";     // Player selecting weapon
        public const string StepBattle = "fight";                   // Active combat in progress
        public const string StepPostBattle = "post_battle";         // After battle resolution
        public const string StepEndBattle = "end_battle";           // Battle concluded, cleanup

        // Player action constants
        public const string ActionFlee = "flee";        // Flee attempt action
        public const string ActionAttack = "attack";    // Attack action

        // Predefined UI messages
        public const string MsgFlee = "You fled. The forest grows quiet.";
        public const string MsgChooseWeapon = "Choose your weapon:";
        public const string MsgBattleOver = "Battle is over!";
        public const string MsgNothingHappens = "Nothing happens...";
        #endregion

        #region === Battle State Tracking ===

        /// <summary>
        /// Thread-safe dictionary tracking active battle states for each player.
        /// Key: Discord user ID | Value: Current battle state (player vs NPC)
        /// 
        /// Used to maintain state across multiple button interactions in a single battle.
        /// </summary>
        public static readonly ConcurrentDictionary<ulong, BattleStateModel> battleStates = new();

        /// <summary>
        /// Retrieves the current battle step/phase for a specific player.
        /// 
        /// Queries the player's battle state to determine which phase they're currently in
        /// (e.g., weapon selection, active combat, post-battle, etc.).
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        /// <returns>
        /// The current step as a string (e.g., "start", "weapon_choice", "fight").
        /// Defaults to StepStart if no step is set or no battle exists for this player.
        /// </returns>
        public static string GetStep(ulong userId) =>
            BattleStateSetup.GetBattleState(userId).Player.Step ?? StepStart;

        /// <summary>
        /// Sets the current battle step/phase for a specific player.
        /// 
        /// Updates both the internal BattleStateModel and the concurrent dictionary.
        /// This transitions the battle to a new phase (e.g., from "weapon_choice" to "fight").
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        /// <param name="step">The new step/phase to set (e.g., "weapon_choice", "fight", "end_battle").</param>
        public static void SetStep(ulong userId, string step)
        {
            // Get current battle state for this user
            var state = BattleStateSetup.GetBattleState(userId);

            // Update the step field
            state.Player.Step = step;

            // Persist to dictionary for other handlers to access
            battleStates[userId] = state;
        }

        #endregion

        #region === Main Switch - Battle Engine Dispatcher ===

        /// <summary>
        /// Main dispatcher function that routes player actions to the appropriate handler
        /// based on the current battle phase.
        /// 
        /// This is the central hub for all battle action processing. It:
        /// 1. Determines current battle step
        /// 2. Logs action details for debugging
        /// 3. Routes to appropriate handler (START, WEAPON_CHOICE, BATTLE, POST_BATTLE, etc.)
        /// 4. Delegates execution to step-specific handlers
        /// 
        /// This method receives all button interactions during combat and ensures they're
        /// processed according to the current battle state machine.
        /// </summary>
        /// <param name="interaction">The Discord socket interaction (button click or response).</param>
        /// <param name="action">The action the player chose (e.g., "attack", "flee").</param>
        /// <param name="weaponId">The weapon ID if the action involves weapon selection.</param>
        /// <remarks>
        /// Action Routing Table:
        /// ┌─────────────────┬──────────────┬─────────────────────┐
        /// │ Current Step    │ Valid Action │ Handler             │
        /// ├─────────────────┼──────────────┼─────────────────────┤
        /// │ START           │ attack/flee  │ HandleStepStart()   │
        /// │ WEAPON_CHOICE   │ weapon_id    │ HandleStepWeaponChoice()│
        /// │ BATTLE (FIGHT)  │ (internal)   │ (battle processor)  │
        /// │ POST_BATTLE     │ continue     │ HandleStepPostBattle()│
        /// │ END_BATTLE      │ (none)       │ (cleanup)           │
        /// └─────────────────┴──────────────┴─────────────────────┘
        /// 
        /// Example Flow:
        /// 1. Player clicks "Attack" button → SetStep(userId, "weapon_choice")
        /// 2. System presents weapon choices
        /// 3. Player clicks weapon button → SetStep(userId, "fight")
        /// 4. Combat is resolved
        /// 5. Result is displayed → SetStep(userId, "post_battle" or "end_battle")
        /// </remarks>
        public static async Task HandleEncounterAction(SocketInteraction interaction, string action, string weaponId)
        {
            // Get user ID and retrieve current battle step
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);

            // Log the current state for debugging
            LogService.Info($">>> [Current step: {currentStep}, action: {action}, weaponId: {weaponId}] <<<\n");

            // Route to appropriate handler based on current step
            switch (currentStep)
            {
                case StepStart:
                    // Handle initial battle setup (weapon selection or flee)
                    await HandleStepStart((SocketMessageComponent)interaction, action);
                    break;

                case StepWeaponChoice:
                    // Handle weapon selection phase
                    await HandleStepWeaponChoice(interaction, weaponId);
                    break;

                case StepBattle:
                    // Battle is processed separately via button interactions
                    break;

                case StepPostBattle:
                    await HandleStepPostBattle(interaction);
                    break;

                case StepEndBattle:
                    await EmbedBuildersEncounter.EmbedEndBattle(interaction);
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
                await EmbedBuildersEncounter.EmbedPreBattle(component);
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
                await EmbedBuildersEncounter.EmbedEndBattle(interaction, playerAttackResult);
                return;
            }

            // --- NPC attack phase ---
            string npcAttackResult = state.NpcWeapons.FirstOrDefault() is { } npcWeapon
                ? NpcAttack.ProcessNpcAttack(userId, npcWeapon)
                : $"⚠️ {state.Npc.Name} has nothing to attack with.";

            // Combine both attack summaries
            string fullAttackLog = $"{playerAttackResult}\n\n{npcAttackResult}";

            // --- Build embed and battle buttons ---
            var battleEmbed = EmbedBuildersEncounter.BuildBattleEmbed(userId, fullAttackLog);
            var battleButtons = EmbedBuildersEncounter.BuildBattleButtons(state);

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
                await EmbedBuildersEncounter.EmbedEndBattle(interaction);
                return;
            }

            // Otherwise, return to weapon choice step
            SetStep(userId, StepWeaponChoice);
        }

        #endregion
    }
}