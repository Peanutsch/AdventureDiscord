using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.Player;
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
    /// Type Safety: Uses BattleStep and PlayerAction enums to prevent string typos and ensure correctness.
    /// 
    /// Usage Pattern:
    /// 1. SetStep(userId, BattleStep.Start) - Initialize battle
    /// 2. HandleEncounterAction() - Route player actions
    /// 3. GetStep(userId) - Check current phase
    /// 4. Clean up when step == BattleStep.EndBattle
    /// </remarks>
    /// </summary>
    public static class EncounterBattleStepsSetup
    {

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
        /// The current BattleStep for the player.
        /// Defaults to BattleStep.Start if no step is set or no battle exists for this player.
        /// </returns>
        public static BattleStep GetStep(ulong userId)
        {
            BattleStateModel? state = BattleStateSetup.GetBattleState(userId);
            if (string.IsNullOrEmpty(state.Player.Step))
                return BattleStep.Start;

            // Convert string to enum
            if (Enum.TryParse<BattleStep>(state.Player.Step, ignoreCase: true, out BattleStep result))
                return result;

            return BattleStep.Start;
        }

        /// <summary>
        /// Sets the current battle step/phase for a specific player.
        /// 
        /// Updates both the internal BattleStateModel and the concurrent dictionary.
        /// This transitions the battle to a new phase (e.g., from WeaponChoice to Battle).
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        /// <param name="step">The new BattleStep phase to set.</param>
        public static void SetStep(ulong userId, BattleStep step)
        {
            // Get current battle state for this user
            BattleStateModel state = BattleStateSetup.GetBattleState(userId);

            // Update the step field (convert enum to string for storage)
            state.Player.Step = step.ToString();

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
        /// │ Start           │ Attack/Flee  │ HandleStepStart()   │
        /// │ WeaponChoice    │ weapon_id    │ HandleStepWeaponChoice()│
        /// │ Battle          │ (internal)   │ (battle processor)  │
        /// │ PostBattle      │ continue     │ HandleStepPostBattle()│
        /// │ EndBattle       │ (none)       │ (cleanup)           │
        /// └─────────────────┴──────────────┴─────────────────────┘
        /// 
        /// Example Flow:
        /// 1. Player clicks "Attack" button → SetStep(userId, BattleStep.WeaponChoice)
        /// 2. System presents weapon choices
        /// 3. Player clicks weapon button → SetStep(userId, BattleStep.Battle)
        /// 4. Combat is resolved
        /// 5. Result is displayed → SetStep(userId, BattleStep.PostBattle or EndBattle)
        /// </remarks>
        public static async Task HandleEncounterAction(SocketInteraction interaction, string action, string weaponId)
        {
            // Get user ID and retrieve current battle step
            ulong userId = interaction.User.Id;
            BattleStep currentStep = GetStep(userId);

            // Log the current state for debugging
            LogService.Info($">>> [Current step: {currentStep}, action: {action}, weaponId: {weaponId}] <<<\n");

            // Route to appropriate handler based on current step
            switch (currentStep)
            {
                case BattleStep.Start:
                    // Handle initial battle setup (weapon selection or flee)
                    await HandleStepStart((SocketMessageComponent)interaction, action);
                    break;

                case BattleStep.WeaponChoice:
                    // Handle weapon selection phase
                    await HandleStepWeaponChoice(interaction, weaponId);
                    break;

                case BattleStep.Battle:
                    // Battle is processed separately via button interactions
                    break;

                case BattleStep.PostBattle:
                    await HandleStepPostBattle(interaction);
                    break;

                case BattleStep.EndBattle:
                    await EmbedBuildersEncounter.EmbedEndBattleInDM(interaction);
                    break;

                default:
                    await interaction.RespondAsync(BattleMessages.NothingHappens, ephemeral: false);
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

            // Get the DM channel and active message first (needed for both Flee and Attack)
            IDMChannel? dmChannel = null;
            IUserMessage? message = null;
            ulong activeMessageId = BattlePrivateMessageHelper.GetActiveBattleMessage(userId);

            if (activeMessageId != 0)
            {
                try
                {
                    dmChannel = await component.User.CreateDMChannelAsync();
                    if (dmChannel != null)
                    {
                        var fetchedMessage = await dmChannel.GetMessageAsync(activeMessageId);
                        if (fetchedMessage is IUserMessage userMsg)
                        {
                            message = userMsg;
                            LogService.Info($"[HandleStepStart] ✅ Retrieved DM message {activeMessageId}");
                        }
                        else
                        {
                            LogService.Error($"[HandleStepStart] ❌ Fetched message is not IUserMessage");
                        }
                    }
                    else
                    {
                        LogService.Error("[HandleStepStart] ❌ DM channel is null");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"[HandleStepStart] ❌ Failed to retrieve DM message: {ex.Message}");
                }
            }
            else
            {
                LogService.Error("[HandleStepStart] ❌ No active message ID found!");
            }

            if (action == PlayerAction.Flee.ToString().ToLower())
            {
                // Player chose to flee → disable buttons and show message
                LogService.Info("[HandleStepStart] Player flees");

                if (message != null)
                {
                    try
                    {
                        // Create flee embed
                        var fleeEmbed = new EmbedBuilder()
                            .WithTitle("🏃 Flee Attempt")
                            .WithDescription(BattleMessages.Flee)
                            .WithColor(Color.Orange)
                            .Build();

                        // Disable all buttons
                        var disabledButtons = new ComponentBuilder(); // Empty = no buttons

                        await BattlePrivateMessageHelper.UpdateBattleMessageAsync(
                            message,
                            fleeEmbed,
                            disabledButtons.Build()); // Empty ComponentBuilder = no components
                        LogService.Info("[HandleStepStart] ✅ Flee message updated in DM with disabled buttons");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"[HandleStepStart] ❌ Failed to update flee message: {ex.Message}");
                    }
                }

                SetStep(userId, BattleStep.Flee);
            }
            else if (action.Equals(PlayerAction.Attack.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                // Player chose attack → disable buttons from encounter embed, then show weapon selection
                LogService.Info("[HandleStepStart] Player chooses attack");

                if (message != null)
                {
                    try
                    {
                        // Disable all buttons but keep the encounter embed
                        var disabledButtons = new ComponentBuilder(); // Empty = no buttons

                        await BattlePrivateMessageHelper.UpdateBattleMessageAsync(
                            message,
                            message.Embeds.FirstOrDefault()?.ToEmbedBuilder().Build() ?? new EmbedBuilder().Build(),
                            disabledButtons.Build()); // Empty ComponentBuilder = no components
                        LogService.Info("[HandleStepStart] ✅ Disabled buttons on encounter embed");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"[HandleStepStart] ❌ Failed to disable buttons: {ex.Message}");
                    }
                }
                else
                {
                    LogService.Error("[HandleStepStart] ❌ Message is null, cannot disable buttons");
                }

                // Now show weapon selection in DM
                LogService.Info("[HandleStepStart] Showing weapons in DM...");
                await EmbedBuildersEncounter.EmbedPreBattleInDM(component);
                SetStep(userId, BattleStep.WeaponChoice);
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
            BattleStateModel state = BattleStateSetup.GetBattleState(userId);
            HashSet<string>? ownedWeaponIds = state.Player.Weapons.Select(w => w.Id).ToHashSet();

            WeaponModel? weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();
            if (weapon == null)
            {
                await interaction.RespondAsync("Je hebt een onbekend wapen gekozen...", ephemeral: true);
                return;
            }

            if (!ownedWeaponIds.Contains(weaponId))
            {
                await interaction.RespondAsync($"Je rommelt met een onbekend wapen: {weapon.Name}...", ephemeral: true);
            }

            // Transition to battle step
            SetStep(userId, BattleStep.Battle);

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
            BattleStateModel state = BattleStateSetup.GetBattleState(userId);

            // --- Increment round counter (once per round) ---
            state.RoundCounter++;

            // --- Validate weapon existence ---
            WeaponModel? weapon = state.PlayerWeapons.FirstOrDefault(w => w.Id == weaponId);
            if (weapon == null)
            {
                await interaction.FollowupAsync("⚠️ Weapon not found in your inventory.", ephemeral: true);
                return;
            }

            // --- Get the active message to disable buttons before showing battle ---
            ulong activeMessageId = BattlePrivateMessageHelper.GetActiveBattleMessage(userId);

            if (activeMessageId != 0)
            {
                try
                {
                    IDMChannel dmChannel = await interaction.User.CreateDMChannelAsync();
                    if (dmChannel != null && await dmChannel.GetMessageAsync(activeMessageId) is IUserMessage message)
                    {
                        // Disable buttons on weapon selection message before showing battle
                        var disabledButtons = new ComponentBuilder(); // Empty = no buttons
                        await BattlePrivateMessageHelper.UpdateBattleMessageAsync(
                            message,
                            message.Embeds.FirstOrDefault()?.ToEmbedBuilder().Build() ?? new EmbedBuilder().Build(),
                            disabledButtons.Build()); // Empty ComponentBuilder = no components
                        LogService.Info("[HandleStepBattle] ✅ Disabled buttons on weapon selection message.");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"[HandleStepBattle] Failed to disable buttons: {ex.Message}");
                }
            }

            // --- Player attack phase ---
            string playerAttackResult = PlayerAttack.ProcessPlayerAttack(userId, weapon);

            // --- Check if NPC is defeated after player's attack ---
            if (state.CurrentHitpointsNPC <= 0 || state.StateOfNPC == "Defeated")
            {
                await SendGuildBattleUpdateAsync(state, playerAttackResult);
                await EmbedBuildersEncounter.EmbedEndBattleInDM(interaction, playerAttackResult, state.PlayerLeveledUp);
                return;
            }

            // --- NPC attack phase ---
            string npcAttackResult = state.NpcWeapons.FirstOrDefault() is { } npcWeapon
                ? NpcAttack.ProcessNpcAttack(userId, npcWeapon)
                : $"⚠️ {state.Npc.Name} has nothing to attack with.";

            // Combine both attack summaries
            string fullAttackLog = $"{playerAttackResult}\n\n{npcAttackResult}";

            // --- Build embed and battle buttons ---
            EmbedBuilder battleEmbed = EmbedBuildersEncounter.BuildBattleEmbed(userId, fullAttackLog);
            ComponentBuilder battleButtons = EmbedBuildersEncounter.BuildBattleButtons(state);

            // --- Send battle as NEW message (separate from weapon selection) ---
            IUserMessage? dmMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                interaction,
                battleEmbed.Build(),
                battleButtons.Build());

            if (dmMessage != null)
            {
                BattlePrivateMessageHelper.SetActiveBattleMessage(userId, dmMessage.Id);
                LogService.Info("[HandleStepBattle] Battle sent as new DM message.");
            }
            else
            {
                LogService.Error("[HandleStepBattle] ❌ Failed to send battle message to DM.");
            }

            // --- Send battle update to guild channel for other members to follow ---
            await SendGuildBattleUpdateAsync(state, fullAttackLog);

            // --- Transition to post-battle step ---
            SetStep(userId, BattleStep.PostBattle);
            await HandleStepPostBattle(interaction);
        }
        /// <summary>
        /// Sends a battle update embed to the guild channel if a guild channel is configured.
        /// </summary>
        /// <param name="state">The current battle state.</param>
        /// <param name="attackLog">The attack summary text to display.</param>
        private static async Task SendGuildBattleUpdateAsync(BattleStateModel state, string attackLog)
        {
            if (state.GuildChannelId == 0)
                return;

            EmbedBuilder guildEmbed = EmbedBuildersEncounter.BuildGuildBattleUpdateEmbed(state, attackLog);
            await BattlePrivateMessageHelper.SendGuildMessageUpdateAsync(state.GuildChannelId, guildEmbed.Build());
        }

        #endregion

        #region === Step: Post Battle ===

        /// <summary>
        /// Handles post-battle logic.
        /// If the NPC is defeated, ends the battle.
        /// Otherwise, stays in the battle embed and updates buttons for next weapon selection.
        /// </summary>
        public static async Task HandleStepPostBattle(SocketInteraction interaction)
        {
            if (interaction == null) return;

            ulong userId = interaction.User.Id;
            BattleStateModel state = BattleStateSetup.GetBattleState(userId);
            if (state == null)
            {
                await interaction.FollowupAsync("No battle found...", ephemeral: true);
                return;
            }

            // End battle if player or NPC is dead
            if (state.Player.Hitpoints <= 0 || state.StateOfNPC == "Defeated")
            {
                SetStep(userId, BattleStep.EndBattle);
                await EmbedBuildersEncounter.EmbedEndBattleInDM(interaction, leveledUp: state.PlayerLeveledUp);
                return;
            }

            // --- Otherwise, stay in battle and update buttons for next weapon selection ---
            SetStep(userId, BattleStep.WeaponChoice);

            // --- Update the DM message with weapon buttons for next round ---
            ulong activeMessageId = BattlePrivateMessageHelper.GetActiveBattleMessage(userId);

            if (activeMessageId != 0)
            {
                try
                {
                    IDMChannel dmChannel = await interaction.User.CreateDMChannelAsync();
                    if (dmChannel != null)
                    {
                        IUserMessage? message = await dmChannel.GetMessageAsync(activeMessageId) as IUserMessage;
                        if (message != null)
                        {
                            // Keep the battle embed but update only the buttons for weapon selection
                            ComponentBuilder weaponButtons = EmbedBuildersEncounter.BuildBattleButtons(state);

                            await BattlePrivateMessageHelper.UpdateBattleMessageAsync(
                                message,
                                message.Embeds.FirstOrDefault()?.ToEmbedBuilder().Build() ?? new EmbedBuilder().Build(),
                                weaponButtons.Build());
                            LogService.Info("[HandleStepPostBattle] Battle continues with updated weapon buttons in DM.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"[HandleStepPostBattle] Failed to update message with weapon buttons: {ex.Message}");
                }
            }
        }

        #endregion
    }
}