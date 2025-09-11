using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Text.Json;
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
using Microsoft.VisualBasic;

namespace Adventure.Quest.Battle
{
    /// <summary>
    /// The BattleEngine manages the turn-based combat system between players and creatures.
    /// It maintains battle state per user and handles key steps such as starting the battle,
    /// selecting weapons, processing attacks, and post-battle outcomes.
    /// </summary>
    public static class BattleEngine
    {
        // Constants for different stages in the battle flow
        public const string StepStart = "start";
        public const string StepFlee = "flee";
        public const string StepWeaponChoice = "weapon_choice";
        public const string StepBattle = "fight";
        public const string StepPostBattle = "post_battle";
        public const string StepEndBattle = "end_battle";

        // Constants for actions
        public const string ActionFlee = "flee";
        public const string ActionAttack = "attack";

        // Messages used in interactions
        public const string MsgFlee = "You fled. The forest grows quiet.";
        public const string MsgChooseWeapon = "Choose your weapon:";
        public const string MsgBattleOver = "Battle is over!";
        public const string MsgNothingHappens = "Nothing happens...";

        // Tracks each user's active battle state
        public static readonly ConcurrentDictionary<ulong, BattleStateModel> battleStates = new();

        /// <summary>
        /// Gets the current step of the user's battle.
        /// </summary>
        public static string GetStep(ulong userId) =>
            GetBattleState(userId).Player.Step ?? StepStart;

        /// <summary>
        /// Updates the battle step for a user.
        /// </summary>
        public static void SetStep(ulong userId, string step)
        {
            var state = GetBattleState(userId);
            state.Player.Step = step;
            battleStates[userId] = state;
        }

        /// <summary>
        /// Retrieves or initializes the battle state for the user.
        /// </summary>
        public static BattleStateModel GetBattleState(ulong userId)
        {
            if (!battleStates.TryGetValue(userId, out var state))
            {
                // Load player data and inventory
                var player = PlayerDataManager.LoadByUserId(userId);

                var weaponIds = player.Weapons.Select(w => w.Id).ToList();
                var armorIds = player.Armor.Select(a => a.Id).ToList();
                var itemIds = player.Items.Select(i => i.Id).ToList();

                var playerWeapons = GameEntityFetcher.RetrieveWeaponAttributes(weaponIds);
                var playerArmor = GameEntityFetcher.RetrieveArmorAttributes(armorIds);
                var playerItems = GameEntityFetcher.RetrieveItemAttributes(itemIds);

                // Add total ammount to Weapons
                foreach (var weapon in player.Weapons)
                {
                    var match = player.Weapons.FirstOrDefault(w => w.Id == weapon.Id);
                    if (match != null)
                        weapon.Value = match.Value;
                }

                // Add total ammount to Armor
                foreach (var armor in player.Armor)
                {
                    var match = player.Armor.FirstOrDefault(a => a.Id == armor.Id);
                    if (match != null)
                        armor.Value = match.Value;
                }

                // Add total ammount to Items
                foreach (var item in player.Items)
                {
                    var match = player.Weapons.FirstOrDefault(i => i.Id == item.Id);
                    if (match != null)
                        item.Value = match.Value;
                }


                // Create new battle state
                state = new BattleStateModel
                {
                    Player = player,
                    Npc = new NpcModel(),
                    PlayerWeapons = playerWeapons,
                    PlayerArmor = playerArmor,
                    Items = playerItems,
                    NpcWeapons = new List<WeaponModel>(),
                    NpcArmor = new List<ArmorModel>(),
                    PrePlayerHP = player.Hitpoints,
                    PreNpcHP = 0,
                    LastUsedWeapon = "",
                    TotalDamage = 0
                };
            }

            return battleStates.GetOrAdd(userId, state);
        }

        /// <summary>
        /// Assigns the creature for the current encounter and loads its weapons and armor.
        /// </summary>
        public static void SetupNpc(ulong userId, NpcModel npc)
        {
            var state = GetBattleState(userId);
            state.Npc = npc;

            // Save NPC stats to BattleState
            var RollHitpointsNPC = ChallengeRatingHelpers.GetNpcHitpoints(npc, npc.CR, userId);
            state.HitpointsAtStartNPC = RollHitpointsNPC; // Store start NPC HP
            state.HitpointsNPC = RollHitpointsNPC; // Track NPC HP
            state.StateOfNPC = "Healthy";
            state.RewardXP = ChallengeRatingHelpers.GetRewardXP(npc.CR);
            LogService.Info($"\n[BattleEngine.SetNpc] NPC: {npc.Name} HP: {state.HitpointsNPC}\n");

            if (npc.Weapons != null)
                state.NpcWeapons = GameEntityFetcher.RetrieveWeaponAttributes(npc.Weapons);

            if (npc.Armor != null)
                state.NpcArmor = GameEntityFetcher.RetrieveArmorAttributes(npc.Armor);

            battleStates[userId] = state;
        }

        /// <summary>
        /// Main handler for user actions during a battle (e.g., attack, flee).
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
                    // Called externally by button interaction handler
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

        private static async Task HandleStepStart(SocketMessageComponent component, string action)
        {
            ulong userId = component.User.Id;
            LogService.DividerParts(1, "HandleStepStart");

            if (action == ActionFlee)
            {
                LogService.Info("[BattleEngine.HandleStepStart] Player flees");

                await component.UpdateAsync(msg =>
                {
                    msg.Content = MsgFlee;
                    msg.Components = new ComponentBuilder().Build(); // knoppen verwijderen
                    msg.Embed = null;
                });

                SetStep(userId, StepFlee);
            }
            else if (action == ActionAttack)
            {
                LogService.Info("[BattleEngine.HandleStepStart] Player choose attack. Calling EncounterService.ShowWeaponChoices...");

                //await component.DeferAsync();
                await EncounterService.PrepareForBattleChoices(component);

                SetStep(userId, StepWeaponChoice);
            }

            LogService.DividerParts(2, "HandleStepStart");
        }


        /// <summary>
        /// Handles the weapon selection step and transitions to the battle step.
        /// </summary>
        private static async Task HandleStepWeaponChoice(SocketInteraction interaction, string weaponId)
        {
            LogService.Info("[Running BattleEngine.HandleStepWeaponChoice]");

            ulong userId = interaction.User.Id;
            var state = GetBattleState(userId);
            var ownedWeaponIds = state.Player.Weapons.Select(w => w.Id).ToHashSet();
            //var inventory = InventoryStateService.GetState(userId).Inventory;

            LogService.DividerParts(1, "HandleStepWeaponChoice");

            WeaponModel? weapon = GameEntityFetcher.RetrieveWeaponAttributes(new List<string> { weaponId }).FirstOrDefault();

            if (weapon == null)
            {
                await interaction.RespondAsync("You selected an unknown weapon...", ephemeral: false);
                LogService.Error("[HandleStepWeaponChoice] Invalid weapon selected.");
                return;
            }

            if (ownedWeaponIds.Contains(weaponId))
            {
                // Valid weapon from player's inventory
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
                // Weapon not in inventory (but exists)
                SetStep(userId, StepBattle);
                await interaction.RespondAsync($"You fumble with the unfamiliar {weapon.Name}...", ephemeral: false);
            }

            LogService.DividerParts(2, "HandleStepWeaponChoice");
        }

        /// <summary>
        /// Processes both player's and creature's attack and updates battle state accordingly.
        /// </summary>
        public static async Task HandleStepBattle(SocketInteraction interaction, string weaponId)
        {
            ulong userId = interaction.User.Id;
            var state = GetBattleState(userId);
            var npc = state.Npc;

            LogService.DividerParts(1, "HandleStepBattle");

            await interaction.DeferAsync();

            var preAttackInfo = $"HP before attack:\nPlayer = {state.Player.Hitpoints}\nCreature = {state.HitpointsNPC}";

            var weapon = state.PlayerWeapons.FirstOrDefault(w => w.Id == weaponId);
            if (weapon == null)
            {
                await interaction.ModifyOriginalResponseAsync(msg =>
                    msg.Content = $"{preAttackInfo}\n\n⚠️ Weapon not found in your inventory.");
                return;
            }

            // 📌 HP opslaan vóór de gevechten
            int prePlayerHP = state.Player.Hitpoints;
            int preNpcHP = state.HitpointsNPC;

            // ⚔️ Speler valt aan
            string playerAttackResult = PlayerAttack.ProcessPlayerAttack(userId, weapon);

            if (state.HitpointsNPC <= 0)
            {
                var embed = EncounterService.RebuildBattleEmbed(
                    userId,
                    prePlayerHP,
                    preNpcHP,
                    playerAttackResult);

                await interaction.ModifyOriginalResponseAsync(msg => msg.Embed = embed.Build());
                return;
            }

            // 📌 Creature heeft geen wapen
            var npcWeapon = state.NpcWeapons.FirstOrDefault();
            if (npcWeapon == null)
            {
                var embed = EncounterService.RebuildBattleEmbed(
                    userId,
                    prePlayerHP,
                    preNpcHP,
                    $"{playerAttackResult}\n\n⚠️ {state.Npc.Name} has nothing to attack with.");

                await interaction.ModifyOriginalResponseAsync(msg => msg.Embed = embed.Build());
                return;
            }

            // 💥 Creature valt terug aan
            string npcAttackResult = NpcAttack.ProcessNpcAttack(userId, npcWeapon);

            // 📦 Combineer output
            string fullAttackLog = $"{playerAttackResult}\n\n{npcAttackResult}";

            // 🧱 Bouw de embed met gecombineerde aanval
            var fullEmbed = EncounterService.RebuildBattleEmbed(
                userId,
                prePlayerHP,
                preNpcHP,
                fullAttackLog);

            // 📬 Update bericht
            await interaction.ModifyOriginalResponseAsync(msg => msg.Embed = fullEmbed.Build());

            SetStep(userId, StepPostBattle);
            await HandleStepPostBattle(interaction);

            LogService.DividerParts(2, "HandleStepBattle");
        }

        /// <summary>
        /// Handles post-battle state update and feedback message.
        /// </summary>
        public static async Task HandleStepPostBattle(SocketInteraction interaction)
        {
            LogService.Info("[RUNNING HandleStepPostBattle]");

            if (interaction == null)
                return;

            ulong userId = interaction.User.Id;
            var state = GetBattleState(userId);

            if (state == null)
            {
                await interaction.RespondAsync("No battle found...");
                return;
            }

            var player = state.Player;
            var npc = state.Npc;

            if (player.Hitpoints <= 0 && state.HitpointsNPC  <= 0)
                SetStep(userId, StepEndBattle);
            else if (player.Hitpoints <= 0)
                SetStep(userId, StepEndBattle);
            else if (state.HitpointsNPC <= 0)
                SetStep(userId, StepEndBattle);
            else
                SetStep(userId, StepWeaponChoice);
        }
    }
}

