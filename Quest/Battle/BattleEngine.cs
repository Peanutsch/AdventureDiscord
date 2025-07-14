using System.Collections.Concurrent;
using System.Linq;
using Adventure.Buttons;
using Adventure.Data;
using Adventure.Helpers;
using Adventure.Models.BattleState;
using Adventure.Models.Creatures;
using Adventure.Models.Items;
using Adventure.Models.Player;
using Adventure.Services;
using Discord;
using Discord.WebSocket;

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
        private const string StepPostBattle = "post_battle";

        // Action identifiers
        private const string ActionFlee = "flee";
        private const string ActionAttack = "attack";

        // Common messages
        private const string MsgFlee = "You fled. The forest grows quiet.";
        private const string MsgChooseWeapon = "Choose your weapon:";
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

        /// <summary>
        /// Gets or initializes the battle state for a user.
        /// </summary>
        public static BattleStateModel GetBattleState(ulong userId)
        {
            if (!battleStates.TryGetValue(userId, out var state))
            {
                var inventory = InventoryStateService.GetState(userId).Inventory;
                var playerWeapons = GameEntityFetcher.RetrieveWeaponAttributes(inventory.Keys.ToList());

                state = new BattleStateModel
                {
                    Player = new PlayerModel(),
                    Creatures = new CreaturesModel(),
                    PlayerWeapons = playerWeapons,
                    PlayerArmor = new List<ArmorModel>(),
                    CreatureWeapons = new List<WeaponModel>(),
                    CreatureArmor = new List<ArmorModel>(),
                };
            }

            return battleStates.GetOrAdd(userId, state);
        }

        /// <summary>
        /// Sets the creature data for the current encounter.
        /// </summary>
        public static void SetCreature(ulong userId, CreaturesModel creature)
        {
            var state = GetBattleState(userId);
            state.Creatures = creature;
            battleStates[userId] = state;
        }

        /// <summary>
        /// Handles actions taken during a battle encounter (e.g., attack or flee).
        /// </summary>
        public static async Task HandleEncounterAction(SocketInteraction interaction, string action, string weaponId)
        {
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);

            switch (currentStep)
            {
                case StepStart:
                    await HandleStepStart(interaction, action);
                    break;

                case StepWeaponChoice:
                    await HandleStepWeaponChoice(interaction, weaponId);
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

            if (action == ActionFlee)
            {
                LogService.Info("[HandleStepStart] Player flees");

                if (interaction is SocketMessageComponent componentFlee)
                    await ButtonInteractionHelpers.RemoveButtonsAsync(componentFlee, MsgFlee);
                else
                    await interaction.RespondAsync(MsgFlee, ephemeral: false);

                SetStep(userId, StepFlee);
            }
            else if (action == ActionAttack)
            {
                LogService.Info("[HandleStepStart] Player attacks");

                if (interaction is SocketMessageComponent componentAttack)
                    await ShowWeaponChoiceButtons(componentAttack, userId);
                else
                    await interaction.RespondAsync(MsgChooseWeapon, ephemeral: false);

                SetStep(userId, StepWeaponChoice);
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

            LogService.Info("[HandleStepWeaponChoice]");

            WeaponModel? weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();

            if (weapon == null)
            {
                await interaction.RespondAsync("You selected an unknown weapon...", ephemeral: false);
                return;
            }

            if (inventory.ContainsKey(weaponId))
            {
                string message = $"You attack with your {weapon.Name}!";
                if (interaction is SocketMessageComponent componentWeaponChoice)
                    await ButtonInteractionHelpers.RemoveButtonsAsync(componentWeaponChoice, message);
                else
                    await interaction.RespondAsync(message, ephemeral: false);

                SetStep(userId, StepPostBattle);
            }
            else
            {
                await interaction.RespondAsync($"You fumble with the unfamiliar {weapon.Name}...", ephemeral: false);
            }
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
                LogService.Info($"[BattleEngine.ShowWeaponChoiceButtons] Label: {weapon.Name!.ToUpper()} ");
                builder.WithButton($"Attack with {weapon.Name!.ToUpper()}", $"_{weapon.Id}", ButtonStyle.Primary);
            }

            await component.UpdateAsync(msg =>
            {
                var embed = component.Message.Embeds.FirstOrDefault()?.ToEmbedBuilder()?.Build();
                msg.Embeds = embed != null ? new[] { embed } : null;
                msg.Content = "";
                msg.Components = builder.Build();
            });
        }

        //
        /*
        public static async Task HandleEncounterAction(SocketInteraction interaction, string action, string weaponId)
        {
            ulong userId = interaction.User.Id;
            string currentStep = GetStep(userId);
            var state = GetBattleState(userId);
            var inventory = InventoryStateService.GetState(userId).Inventory;

            switch (currentStep)
            {
                case StepStart:
                    if (action == ActionFlee)
                    {
                        LogService.Info($"\n[BattleEngine.HandleEncounterAction] Running case StepStart/ActionFlee");

                        if (interaction is SocketMessageComponent componentFlee)
                            await ButtonInteractionHelpers.RemoveButtonsAsync(componentFlee, MsgFlee);
                        else
                        {
                            await interaction.RespondAsync(MsgFlee, ephemeral: false);
                        }

                        SetStep(userId, StepFlee);
                    }
                    else if (action == ActionAttack)
                    {

                        LogService.Info($"\n[BattleEngine.HandleEncounterAction] Running case StepStart/ActionAttack");

                        if (interaction is SocketMessageComponent componentAttack)
                        {
                            var builder = new ComponentBuilder();
                            var weaponIds = inventory.Keys.Select(k => k.ToLower()).ToList();
                            var weapons = GameEntityFetcher.RetrieveWeaponAttributes(weaponIds);
                            state.PlayerWeapons = weapons; // 🔄 update opgeslagen wapens

                            foreach (var weapon in weapons)
                            {
                                string customId = $"_{weapon.Id}";
                                string label = $"Attack with {weapon.Name!.ToUpper()}";

                                LogService.Info($"[BattleEngine.HandleEncounterAction] StepStart/ActionAttack > customId: [{customId}] label: [{label}]");

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
                    LogService.Info($"\n[BattleEngine.HandleEncounterAction] Running case StepWeaponChoice");

                    WeaponModel? weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();
                    LogService.Info($"[FightEngine.HandleEncounterAction] > [Case StepWeaponChoice] > Chosen weapon: {weapon?.Name}");

                    if (weapon == null)
                    {
                        await interaction.RespondAsync("You selected an unknown weapon...", ephemeral: false);
                        return;
                    }

                    //if (inventory.ContainsKey(weapon.Name!.ToLower()))
                    if (inventory.ContainsKey(weaponId))
                    {
                        string weaponUsed = $"You attack with your {weapon.Name}!";

                        if (interaction is SocketMessageComponent componentWeaponChoice)
                            await ButtonInteractionHelpers.RemoveButtonsAsync(componentWeaponChoice, weaponUsed);
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
        */
        //

        /// <summary>
        /// Processes the player's attack and applies damage to the creature.
        /// </summary>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            var state = GetBattleState(userId);
            var creature = state.Creatures;
            var random = new Random();

            int damage = 0; // Placeholder — logic for actual damage calculation to be implemented

            // Reduce creature HP
            creature.Hitpoints -= damage;
            if (creature.Hitpoints < 0)
                creature.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessPlayerAttack] {state.Player.Name} attacked {creature.Name} for {damage} damage. Remaining HP: {creature.Hitpoints}");

            if (creature.Hitpoints <= 0)
            {
                SetStep(userId, StepPostBattle);
                return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** for `{damage}` damage.\n💀 The creature is defeated!";
            }

            return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** for `{damage}` damage.\n🧟 {creature.Name} has `{creature.Hitpoints}` HP left.";
        }

        /// <summary>
        /// Processes the creature's attack on the player.
        /// </summary>
        public static string ProcessCreatureAttack(ulong userId)
        {
            var state = GetBattleState(userId);
            var player = state.Player;
            var creature = state.Creatures;

            var random = new Random();
            //int damage = random.Next(5, 11); // Creature deals between 5 and 10 damage
            int damage = 0;

            player.Hitpoints -= damage;
            if (player.Hitpoints < 0)
                player.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} attacked {player.Name} for {damage} damage. Remaining HP: {player.Hitpoints}");

            if (player.Hitpoints <= 0)
            {
                SetStep(userId, StepPostBattle);
                return $"💥 **{creature.Name}** attacked you for `{damage}` damage.\n☠️ You have been defeated!";
            }

            return $"💥 **{creature.Name}** attacked you for `{damage}` damage.\n❤️ You have `{player.Hitpoints}` HP left.";
        }
    }
}
