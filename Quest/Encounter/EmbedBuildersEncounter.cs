using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Modules;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Encounter
{
    public class EmbedBuildersEncounter
    {
        #region === Embed Random Encounter ===
        /// <summary>
        /// Builds an embed describing the randomly encountered npc with stats and equipment.
        /// </summary>
        /// <param name="npc">The npc to display in the encounter embed.</param>
        /// <returns>An EmbedBuilder with npc details formatted.</returns>
        public static EmbedBuilder EmbedRandomEncounter(NpcModel npc)
        {
            LogService.DividerParts(1, "Data NPC");
            LogService.Info($"[EmbedBuilders.GetRandomEncounter] > Encountered: [{npc.Name}]");

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⚔️ Encounter")
                .WithThumbnailUrl($"{npc.ThumbnailNpc_100}")
                .WithDescription($"**[{npc.Name!.ToUpper()}]**\n*{npc.Description}*");

            AddArmorFields(embed, npc);
            AddWeaponFields(embed, npc);

            LogService.DividerParts(2, "Data NPC");
            return embed;
        }

        /// <summary>
        /// Adds armor fields to the encounter embed, showing the NPC's equipped armor pieces.
        /// </summary>
        /// <param name="embed">The EmbedBuilder to which the armor fields will be added.</param>
        /// <param name="npc">The NPC model containing armor data.</param>
        private static void AddArmorFields(EmbedBuilder embed, NpcModel npc)
        {
            // Log armor data for debugging and tracking
            LogService.Info($"[EmbedBuilders.AddArmorFields] > Armor: {string.Join(",", npc.Armor ?? new())}");

            // If the NPC has no armor data, display "None" in the embed
            if (npc.Armor?.Any() != true)
            {
                embed.AddField("Armor:", "None", false);
                return;
            }

            // Retrieve detailed armor attributes using the NPC's armor IDs/names
            var armorList = GameEntityFetcher.RetrieveArmorAttributes(npc.Armor);

            // If no armor attributes could be found, log an error and add "None"
            if (armorList.Count == 0)
            {
                LogService.Error("[EmbedBuilders.AddArmorFields] > No armor data resolved.");
                embed.AddField("Armor:", "None", false);
                return;
            }

            // Add a separate field for each armor piece with its name, type, and description
            foreach (var armor in armorList)
            {
                embed.AddField($"**[{armor.Name}]**",
                    $"Type: {armor.Type} armor\n" +  // Display armor type (e.g., light, heavy)
                    $"*{armor.Description}*", false); // Italicized description for flavor
            }
        }

        /// <summary>
        /// Adds weapon fields to the encounter embed, displaying each weapon's range and description.
        /// </summary>
        /// <param name="embed">The EmbedBuilder to which the weapon fields will be added.</param>
        /// <param name="npc">The NPC model containing weapon data.</param>
        private static void AddWeaponFields(EmbedBuilder embed, NpcModel npc)
        {
            // Log weapon data for debugging and tracking
            LogService.Info($"[EmbedBuilders.AddWeaponFields] > Weapons: {string.Join(",", npc.Weapons ?? new())}");

            // If the NPC has no weapons, display "None" in the embed
            if (npc.Weapons?.Any() != true)
            {
                embed.AddField("Weapons:", "None", false);
                return;
            }

            // Retrieve detailed weapon attributes using the NPC's weapon IDs/names
            var weaponList = GameEntityFetcher.RetrieveWeaponAttributes(npc.Weapons!);

            // If no weapon data was found, log an error and display "None"
            if (weaponList.Count == 0)
            {
                LogService.Error("[EmbedBuilders.AddWeaponFields] > weaponList = 0");
                embed.AddField("Weapons:", "None", false);
                return;
            }

            // Add a separate field for each weapon with its name, range, and description
            foreach (var weapon in weaponList)
            {
                embed.AddField($"**[{weapon.Name}]**",
                    //$"Range: {weapon.Range} meter\n" +  // Show attack range
                    $"*{weapon.Description}*", false);  // Italicized description for style
            }
        }
        #endregion Embed Random Encounter

        #region === Embed PreBattle ===
        /// <summary>
        /// Displays the pre-battle preparation screen where the player can review their current gear,
        /// stats, and available weapons before the fight begins.  
        /// This method updates the existing Discord message with a new embed and weapon selection buttons.
        /// </summary>
        /// <param name="interaction">The Discord interaction triggered by the player's button click.</param>
        public static async Task EmbedPreBattle(SocketInteraction interaction)
        {
            // --- Validate the interaction type ---
            if (interaction is not SocketMessageComponent component)
            {
                await interaction.RespondAsync("❌ Unable to update pre-battle screen: invalid interaction type.", ephemeral: true);
                return;
            }

            // --- Retrieve the current battle state for the user ---
            var state = BattleStateSetup.GetBattleState(component.User.Id);
            if (state == null)
            {
                LogService.Error("[EmbedBuilders.EmbedPreBattle] > Battle state not found.");
                await component.RespondAsync("❌ No active battle found.", ephemeral: true);
                return;
            }

            // --- Build the pre-battle UI elements ---
            var buttonBuilder = BuildBattleButtons(state);
            var embed = BuildPreBattleEmbed(state);

            try
            {
                // --- Replaces the previous content with the new pre-battle embed and buttons. ---
                await component.UpdateAsync(msg =>
                {
                    msg.Content = string.Empty;
                    msg.Embed = embed.Build();
                    msg.Components = buttonBuilder.Build();
                });

                LogService.Info("[EmbedBuilders.EmbedPreBattle] Embed successfully updated via UpdateAsync.");
            }
            catch (Exception ex)
            {
                // --- Fallback ---
                LogService.Info($"[EmbedBuilders.EmbedPreBattle] UpdateAsync failed, fallback to FollowupAsync. {ex.Message}");
                await component.FollowupAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: false);
            }
        }

        /// <summary>
        /// Builds all the interactive Discord buttons for the pre-battle screen,
        /// including weapon, item, and flee options.
        /// </summary>
        /// <param name="state">The current player's battle state.</param>
        /// <returns>A ComponentBuilder containing all buttons.</returns>
        public static ComponentBuilder BuildBattleButtons(BattleStateModel state)
        {
            var builder = new ComponentBuilder();

            // --- Add a button for each weapon the player currently owns --- 
            if (state.PlayerWeapons != null && state.PlayerWeapons.Any())
            {
                foreach (var weapon in state.PlayerWeapons)
                    builder.WithButton(weapon.Name, weapon.Id, ButtonStyle.Primary);
            }

            // --- Add a button for each item (like potions or consumables) --- 
            if (state.Items != null && state.Items.Any())
            {
                foreach (var item in state.Items)
                    builder.WithButton(item.Name, item.Id, ButtonStyle.Success, row: 2);
            }
            else
            {
                LogService.Info("[EmbedBuilders.EmbedPreBattle] > No items available.");
            }

            // --- Add the 'Flee' or 'Break' button to allow exiting the battle --- 
            builder.WithButton("Flee", $"battle_flee_{state.Player.Id}", ButtonStyle.Secondary);

            return builder;
        }

        /// <summary>
        /// Builds the pre-battle embed containing player stats, equipped weapons, armor, and items.
        /// </summary>
        /// <param name="state">The current player's battle state.</param>
        /// <returns>An EmbedBuilder containing the formatted pre-battle view.</returns>
        private static EmbedBuilder BuildPreBattleEmbed(BattleStateModel state)
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("[Prepare for Battle]")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1425077955121381427/weaponrack2.jpg?ex=68e646c5&is=68e4f545&hm=0f79e4a059c952bda3811786473f42769493eaea55fc71105b2925024727022c&")
                .AddField($"**{state.Player.Name}** prepares for battle...",
                          $"| Level: {state.Player.Level} | XP: {state.Player.XP} | HP: {state.Player.Hitpoints} |");

            // --- Add detailed sections for weapons, armor, and items --- 
            AddWeaponFields(embed, state);
            AddArmorFields(embed, state);
            AddItemFields(embed, state);

            return embed;
        }

        /// <summary>
        /// Adds weapon details to the pre-battle embed, including damage dice and descriptions.
        /// </summary>
        /// <param name="embed">The embed builder to append fields to.</param>
        /// <param name="state">The current player's battle state.</param>
        private static void AddWeaponFields(EmbedBuilder embed, BattleStateModel state)
        {
            foreach (var weapon in state.PlayerWeapons ?? Enumerable.Empty<WeaponModel>())
            {
                string diceNotation = $"{weapon.Damage.DiceCount}d{weapon.Damage.DiceValue}";
                string nameNotation = $"[{weapon.Name} ({diceNotation})]";
                embed.AddField(nameNotation, $"*{weapon.Description}*");
            }
        }

        /// <summary>
        /// Adds armor details to the pre-battle embed, showing the armor class and description.
        /// </summary>
        /// <param name="embed">The embed builder to append fields to.</param>
        /// <param name="state">The current player's battle state.</param>
        private static void AddArmorFields(EmbedBuilder embed, BattleStateModel state)
        {
            foreach (var armor in state.PlayerArmor ?? Enumerable.Empty<ArmorModel>())
            {
                string acNotation = $"Armor Class: {armor.ArmorClass}";
                string nameNotation = $"[{armor.Name} ({acNotation})]";
                embed.AddField(nameNotation, $"*{armor.Description}*");
            }
        }

        /// <summary>
        /// Adds item details to the pre-battle embed, including healing or bonus effects.
        /// </summary>
        /// <param name="embed">The embed builder to append fields to.</param>
        /// <param name="state">The current player's battle state.</param>
        private static void AddItemFields(EmbedBuilder embed, BattleStateModel state)
        {
            foreach (var item in state.Items ?? Enumerable.Empty<ItemModel>())
            {
                string diceNotation = $"{item.Effect.DiceCount}d{item.Effect.DiceValue}+{item.Effect.BonusHP}";
                string nameNotation = $"[{item.Name} ({diceNotation})]";
                embed.AddField(nameNotation, $"*{item.Description}*");
            }
        }
        #endregion Embed PreBattle

        #region === Embed Battle ===
        /// <summary>
        /// Builds the battle embed for the current round without updating the Discord message.
        /// This method:
        /// - Displays attack summaries and current HP for both combatants
        /// - Checks if the player or NPC has been defeated
        /// - Returns the EmbedBuilder for the outer handler to send/update
        /// </summary>
        /// <param name="userId">The player's Discord ID.</param>
        /// <param name="attackSummary">Formatted attack summary for the current round.</param>
        /// <returns>An EmbedBuilder representing the current battle state.</returns>
        public static EmbedBuilder BuildBattleEmbed(ulong userId, string attackSummary)
        {
            // --- Retrieve the current battle state --- 
            var state = BattleStateSetup.GetBattleState(userId);
            var player = state.Player;
            var npc = state.Npc;

            // --- Check for battle end (player or NPC HP <= 0) ---
            if (player.Hitpoints <= 0 || state.CurrentHitpointsNPC <= 0)
            {
                LogService.Info($"[EmbedBuilders.BuildBattleEmbed] Battle ended. Player HP: {player.Hitpoints}, NPC HP: {state.CurrentHitpointsNPC}");

                // Return a simple "battle ended" embed; the outer method should call EmbedEndBattle()
                return new EmbedBuilder()
                    .WithTitle("⚰️ Battle Ended")
                    .WithDescription("The fight is over. One side has fallen...");
            }

            // --- Increment round counter ---
            state.RoundCounter++;

            // --- Determine NPC thumbnail based on HP percentage --- 
            string thumbUrl = HPStatusHelpers.GetNpcThumbnailByHP(npc, state.PercentageHpNpc);

            // --- Build the ongoing battle embed ---
            var embed = new EmbedBuilder()
                .WithColor(state.EmbedColor)
                .WithTitle($"⚔️ Battle Report — Round {state.RoundCounter}")
                .WithThumbnailUrl(thumbUrl)
                .AddField(
                    $"{player.Name} ({state.StateOfPlayer}) VS {npc.Name} ({state.StateOfNPC})",
                    $"| Level {player.Level} | {player.XP} XP | {player.Hitpoints} HP |")
                .AddField("🩸 Attack Summary", attackSummary, false)
                .WithFooter($"Round {state.RoundCounter} completed.");

            return embed;
        }
        #endregion Embed Battle

        #region === Embed End Battle ===
        /// <summary>
        /// Ends the battle.
        /// Removes buttons and replaces the original message with
        /// a final embed containing player stats and battle log.
        /// </summary>
        public static async Task EmbedEndBattle(SocketInteraction interaction, string? extraMessage = null)
        {
            ulong userId = interaction.User.Id;
            var state = BattleStateSetup.GetBattleState(userId);
            string thumbUrl = HPStatusHelpers.GetNpcThumbnailByHP(state.Npc, state.PercentageHpNpc);

            string finalLog = extraMessage ?? "";
            string battleOverText = $"{EncounterBattleStepsSetup.MsgBattleOver}";

            // --- Increment round counter ---
            state.RoundCounter++;

            var embed = new EmbedBuilder()
                .WithColor(state.EmbedColor)
                .WithTitle($"[Battle Report, Round {state.RoundCounter}]")
                .WithThumbnailUrl(thumbUrl) //("https://cdn.discordapp.com/attachments/1425057075314167839/1425079786795176007/skull_dead.jpg")
                .AddField($"{state.Player.Name} (HP: {state.Player.Hitpoints}) VS {state.Npc.Name} ({state.StateOfNPC})",
                          $"| Level {state.Player.Level} | {state.Player.XP} XP | {state.Player.Hitpoints} HP |")
                .AddField("\u200B", $"{finalLog}\n\n{battleOverText}");

            var buttons = new ComponentBuilder()
                .WithButton("CONTINUE", $"battle_continue_{userId}", ButtonStyle.Success);

            if (interaction is SocketMessageComponent component)
            {
                await component.UpdateAsync(msg =>
                {
                    msg.Embed = embed.Build();
                    msg.Components = buttons.Build(); // Add button CONTINUE
                    //msg.Components = new ComponentBuilder().Build(); // remove buttons
                    msg.Content = string.Empty;
                });
            }

            // Reset round counter
            state.RoundCounter = 0;

            // Update battle step
            EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepEndBattle);
        }
        #endregion Embed End Battle
    }
}