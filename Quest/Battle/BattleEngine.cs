using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Adventure.Buttons;
using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Creatures;
using Adventure.Models.Items;
using Adventure.Models.Player;
using Adventure.Quest.Encounter;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.VisualBasic;

namespace Adventure.Quest.Battle
{
    /// <summary>
    /// Handles all battle logic for player and creature encounters.
    /// Maintains battle state per user and provides support for actions like attack, flee, and weapon selection.
    /// </summary>
    public static class BattleEngine
    {
        // Step identifiers for battle progression
        private const string StepStart = "start";
        private const string StepFlee = "flee";
        private const string StepWeaponChoice = "weapon_choice";
        private const string StepBattle = "fight";
        public const string StepPostBattle = "post_battle";
        public const string StepEndBattle = "end_battle";

        // Action identifiers
        private const string ActionFlee = "flee";
        private const string ActionAttack = "attack";

        // Common messages
        private const string MsgFlee = "You fled. The forest grows quiet.";
        private const string MsgChooseWeapon = "Choose your weapon:";
        public const string MsgBattleOver = "Battle is over!";
        private const string MsgNothingHappens = "Nothing happens...";

        // Tracks the battle state per user
        private static readonly ConcurrentDictionary<ulong, BattleStateModel> battleStates = new();

        /// <summary>
        /// Gets the current battle step for the user.
        /// </summary>
        public static string GetStep(ulong userId) =>
            GetBattleState(userId).Player.Step ?? StepStart;

        /// <summary>
        /// Sets the current battle step for the user.
        /// </summary>
        public static void SetStep(ulong userId, string step)
        {
            var state = GetBattleState(userId);
            state.Player.Step = step;
            battleStates[userId] = state;
        }

        public static BattleStateModel GetBattleState(ulong userId)
        {
            if (!battleStates.TryGetValue(userId, out var state))
            {
                var player = PlayerDataManager.LoadByUserId(userId);
                var inventory = InventoryStateService.GetState(userId).Inventory;
                var playerWeapons = GameEntityFetcher.RetrieveWeaponAttributes(inventory.Keys.ToList());

                state = new BattleStateModel
                {
                    Player = player,
                    Creatures = new CreaturesModel(),
                    PlayerWeapons = playerWeapons,
                    PlayerArmor = new List<ArmorModel>(),
                    CreatureWeapons = new List<WeaponModel>(),
                    CreatureArmor = new List<ArmorModel>(),

                    PrePlayerHP = player.Hitpoints,
                    PreCreatureHP = 0,
                    LastUsedWeapon = "",
                    Damage = 0
                };
            }

            return battleStates.GetOrAdd(userId, state);
        }

        public static void SaveBattleState(ulong userId, BattleStateModel newState)
        {
            string json = JsonSerializer.Serialize(newState, new JsonSerializerOptions { WriteIndented = true });
            string path = $"Data/Player/{userId}.json";
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Sets the creature data for the current encounter.
        /// </summary>
        public static void SetCreature(ulong userId, CreaturesModel creature)
        {
            var state = GetBattleState(userId);
            state.Creatures = creature;

            if (creature.Weapons != null)
                state.CreatureWeapons = GameEntityFetcher.RetrieveWeaponAttributes(creature.Weapons);

            if (creature.Armor != null)
                state.CreatureArmor = GameEntityFetcher.RetrieveArmorAttributes(creature.Armor);

            battleStates[userId] = state;
        }

        /// <summary>
        /// Handles actions taken during a battle encounter (e.g., attack or flee).
        /// </summary>
        public static async Task HandleEncounterAction(SocketInteraction interaction, string action, string weaponId)
        {
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);

            LogService.Info($">>> [Current step: {currentStep}, action: {action}, weaponId: {weaponId}] <<<\n");

            switch (currentStep)
            {
                case StepStart:
                    await HandleStepStart(interaction, action);
                    break;

                case StepWeaponChoice:
                    await HandleStepWeaponChoice(interaction, weaponId);
                    break;

                case StepBattle:
                    //await HandleStepBattle(interaction, weaponId);
                    // HandleStepBattle(interaction, weaponId) called in ComponentInteractions.HandleWeaponButton(string weaponId)
                    break;

                case StepPostBattle:

                    LogService.Info("Calling Case HandleEncounterAction.StepPostBattle");

                    await HandleStepPostBattle(interaction);
                    break;

                case StepEndBattle:
                    await interaction.RespondAsync(MsgBattleOver);
                    break;

                default:
                    await interaction.RespondAsync(MsgNothingHappens, ephemeral: false);
                    break;
            }
        }

        /// <summary>
        /// Handles the start step of a battle. Supports attack or flee actions.
        /// </summary>
        private static async Task HandleStepStart(SocketInteraction interaction, string action)
        {
            ulong userId = interaction.User.Id;

            LogService.DividerParts(1, "HandleStepStart");

            if (action == ActionFlee)
            {
                LogService.Info("[HandleStepStart] Player flees");

                if (interaction is SocketMessageComponent componentFlee)
                    await ButtonInteractionHelpers.RemoveButtonsAsync(componentFlee, MsgFlee);
                else
                    await interaction.RespondAsync(MsgFlee, ephemeral: false);

                SetStep(userId, StepFlee);

                LogService.DividerParts(2, "HandleStepStart");
            }
            else if (action == ActionAttack)
            {
                LogService.Info("[HandleStepStart] Player choose attack");

                if (interaction is SocketMessageComponent componentAttack)
                {
                    LogService.Info("[HandleStepStart] Interaction is componentAttack...");
                    await ShowWeaponChoiceButtons(componentAttack, userId);
                }
                else
                {
                    LogService.Error("[HandleStepStart] No component interaction...");
                    await interaction.RespondAsync(MsgChooseWeapon, ephemeral: false);
                }

                SetStep(userId, StepWeaponChoice);

                LogService.DividerParts(2, "HandleStepStart");
            }
        }

        /// <summary>
        /// Handles the step where the user selects a weapon to attack with.
        /// </summary>
        private static async Task HandleStepWeaponChoice(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = GetBattleState(userId);
            var inventory = InventoryStateService.GetState(userId).Inventory;

            LogService.DividerParts(1, "HandleStepWeaponChoice");

            WeaponModel? weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();

            if (weapon == null)
            {
                LogService.Error("[HandleStepWeaponChoice] weapon is null or empty: You selected an unknown weapon...");
                await interaction.RespondAsync("You selected an unknown weapon...", ephemeral: false);
                return;
            }

            if (inventory.ContainsKey(weaponId))
            {
                string message = $"You attack with your {weapon.Name}!";
                if (interaction is SocketMessageComponent componentWeaponChoice)
                {
                    LogService.Info($"[HandleStepWeaponChoice] interaction is SocketMessageComponent componentWeaponChoice: [{message}]");

                    SetStep(userId, StepBattle);
                    await ButtonInteractionHelpers.RemoveButtonsAsync(componentWeaponChoice, message);
                }
                else
                {
                    LogService.Info($"[HandleStepWeaponChoice] interaction is NOT SocketMessageComponent componentWeaponChoice: [{message}]");

                    SetStep(userId, StepBattle);
                    await interaction.RespondAsync(message, ephemeral: false);
                }

                LogService.DividerParts(2, "HandleStepWeaponChoice");
            }
            else
            {
                LogService.Error($"[HandleStepWeaponChoice] Inventory does not contain {weaponId}: [You fumble with the unfamiliar {weapon.Name}...]");

                SetStep(userId, StepBattle);
                await interaction.RespondAsync($"You fumble with the unfamiliar {weapon.Name}...", ephemeral: false);

                LogService.DividerParts(2, "HandleStepWeaponChoice");
            }
        }

        public static async Task HandleStepBattle(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = GetBattleState(userId);
            var creature = state.Creatures;

            LogService.DividerParts(1, "HandleStepBattle");

            LogService.Info($"[BattleEngine.HandleStepBattle] HP before attack:\n" +
                            $"Player = {state.Player.Hitpoints}\n" +
                            $"Creature = {state.Creatures.Hitpoints}");

            // Defer the response to allow updates later
            await interaction.DeferAsync();

            // Info vóór de aanval
            string preAttackInfo = $"HP before attack:\n" +
                                   $"Player = {state.Player.Hitpoints}\n" +
                                   $"Creature = {state.Creatures.Hitpoints}";

            // 1. Zoek het gekozen wapen in de player's inventory
            var weapon = state.PlayerWeapons.FirstOrDefault(w => w.Id == weaponId);
            if (weapon == null)
            {
                await interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"{preAttackInfo}\n\n⚠️ Weapon not found in your inventory.");

                LogService.Error("[HandleStepBattle] weapon is null or empty: Weapon not found in your inventory.");
                LogService.DividerParts(2, "HandleStepBattle");
                return;
            }

            // 2. Speler valt aan
            string playerAttackResult = BattleEngineHelpers.ProcessPlayerAttack(userId, weapon);

            // 3. Check of het wezen dood is
            if (creature.Hitpoints <= 0)
            {
                await interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"{preAttackInfo}\n\n{playerAttackResult}");

                return;
            }

            // 4. Kies het wapen van het wezen
            var creatureWeapon = state.CreatureWeapons.FirstOrDefault();
            if (creatureWeapon == null)
            {
                await interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"{preAttackInfo}\n\n{playerAttackResult}\n\n⚠️ {creature.Name} has nothing to attack with.");

                return;
            }

            // 5. Wezen valt terug aan
            string creatureAttackResult = BattleEngineHelpers.ProcessCreatureAttack(userId, creatureWeapon);

            // 6. Toon gecombineerde resultaten
            string combinedResult = $"{preAttackInfo}\n\n{playerAttackResult}\n\n{creatureAttackResult}";

            await interaction.ModifyOriginalResponseAsync(msg =>
                msg.Content = combinedResult);

            SetStep(userId, StepPostBattle);

            await HandleStepPostBattle(interaction);

            LogService.DividerParts(2, "HandleStepBattle");
        }

        public static async Task HandleStepPostBattle(SocketInteraction interaction)
        {
            LogService.Info("[RINNING HandleStepPostBattle]");

            if (interaction == null)
                return;

            ulong userId = interaction.User.Id;
            var state = GetBattleState(userId);

            if (state == null)
            {
                await interaction.RespondAsync("No battle found...", ephemeral: true);
                return;
            }

            var player = state.Player;
            var npc = state.Creatures;

            // Kies embed-title en vervolg-stap op basis van HP
            string title;
            string description;

            if (player.Hitpoints <= 0 && npc.Hitpoints <= 0)
            {
                title = "⚔️ Double KO ⚔️";
                description = $"Both **{player.Name}** and **{npc.Name}** have fallen!";
                SetStep(userId, StepEndBattle);
            }
            else if (player.Hitpoints <= 0)
            {
                title = "☠️ You were defeated ☠️";
                description = $"The battle between **{player.Name}** and **{npc.Name}** is over. You lost.";
                SetStep(userId, StepEndBattle);
            }
            else if (npc.Hitpoints <= 0)
            {
                title = "🎉 Victory! 🎉";
                description = $"The battle between **{player.Name}** and **{npc.Name}** is over. You won!";
                SetStep(userId, StepEndBattle);
            }
            else
            {
                title = $"⚔️ {player.Name} VS {npc.Name} ⚔️";
                description = $"The fight rages on between **{player.Name}** and **{npc.Name}**!";
                SetStep(userId, StepWeaponChoice);
            }

            // Build Embed
            var embed = EncounterService.RebuildBattleEmbed(userId, state.LastUsedWeapon, state.Damage, player.Hitpoints, npc.Hitpoints)
                .WithTitle(title)
                .WithDescription(description);

            await interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        /// <summary>
        /// Displays available weapon choices as buttons for the user to select.
        /// </summary>
        private static async Task ShowWeaponChoiceButtons(SocketMessageComponent component, ulong userId)
        {
            var state = GetBattleState(userId);
            var inventory = InventoryStateService.GetState(userId).Inventory;
            var weaponIds = inventory.Keys.Select(k => k.ToLower()).ToList();
            var weapons = GameEntityFetcher.RetrieveWeaponAttributes(weaponIds);
            state.PlayerWeapons = weapons;

            var builder = new ComponentBuilder();
            foreach (var weapon in weapons)
            {
                var label = $"{weapon.Name!.ToUpper()}";
                var weaponId = $"{weapon.Id}";

                LogService.Info($"[BattleEngine.ShowWeaponChoiceButtons] Label: Attack with {weapon.Name!.ToUpper()} WeaponId: {weaponId}");
                builder.WithButton($"Attack with {label}", weaponId, ButtonStyle.Primary);
            }

            await component.UpdateAsync(msg =>
            {
                var embed = component.Message.Embeds.FirstOrDefault()?.ToEmbedBuilder()?.Build();
                msg.Embeds = embed != null ? new[] { embed } : null;
                msg.Content = "";
                msg.Components = builder.Build();
            });
        }
    }
}
