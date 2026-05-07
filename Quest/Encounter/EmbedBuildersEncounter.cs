using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Modules;
using Adventure.Modules.Helpers;
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
            LogService.DividerParts(1, "Embed Data NPC");
            LogService.Info($"[EmbedBuilders.GetRandomEncounter] > Encountered: [{npc.Name}]");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⚔️ Encounter")
                .WithThumbnailUrl($"{npc.ThumbHpNpc_100}")
                .WithDescription($"**[{npc.Name!.ToUpper()}]**\n*{npc.Description}*");

            AddArmorFields(embed, npc);
            AddWeaponFields(embed, npc);

            LogService.DividerParts(2, "Embed Data NPC");
            return embed;
        }

        #endregion Embed Random Encounter

        #region === Thumbnail Validation ===

        /// <summary>
        /// Validates if a thumbnail URL has a valid protocol (http://, https://, or attachment://).
        /// Returns false for null, empty, or URLs without proper protocol to prevent Discord embed errors.
        /// </summary>
        /// <param name="thumbnailUrl">The thumbnail URL to validate.</param>
        /// <returns>True if URL is valid, false otherwise.</returns>
        private static bool IsValidThumbnailUrl(string? thumbnailUrl)
        {
            if (string.IsNullOrWhiteSpace(thumbnailUrl))
                return false;

            // Check if URL contains valid protocol
            if (thumbnailUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                thumbnailUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                thumbnailUrl.StartsWith("attachment://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // URL is invalid or missing protocol
            LogService.Info($"[EmbedBuildersEncounter.IsValidThumbnailUrl] Invalid thumbnail URL: {thumbnailUrl}. Skipping thumbnail.");
            return false;
        }

        #endregion Thumbnail Validation

        #region === Embed Armor Fields ===
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
            List<ArmorModel> armorList = GameEntityFetcher.RetrieveArmorAttributes(npc.Armor);

            // If no armor attributes could be found, log an error and add "None"
            if (armorList.Count == 0)
            {
                LogService.Error("[EmbedBuilders.AddArmorFields] > No armor data resolved.");
                embed.AddField("Armor:", "None", false);
                return;
            }

            // Add a separate field for each armor piece with its name, type, and description
            foreach (ArmorModel armor in armorList)
            {
                embed.AddField($"**[{armor.Name}]**",
                    $"Type: {armor.Type} armor\n" +  // Display armor type (e.g., light, heavy)
                    $"*{armor.Description}*", false); // Italicized description for flavor
            }
        }
        #endregion

        #region === Embed Weapon Fields ===
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
            List<WeaponModel> weaponList = GameEntityFetcher.RetrieveWeaponAttributes(npc.Weapons!);

            // If no weapon data was found, log an error and display "None"
            if (weaponList.Count == 0)
            {
                LogService.Error("[EmbedBuilders.AddWeaponFields] > weaponList = 0");
                embed.AddField("Weapons:", "None", false);
                return;
            }

            // Add a separate field for each weapon with its name, range, and description
            foreach (WeaponModel weapon in weaponList)
            {
                embed.AddField($"**[{weapon.Name}]**",
                    //$"Range: {weapon.Range} meter\n" +  // Show attack range
                    $"*{weapon.Description}*", false);  // Italicized description for style
                }
            }
        #endregion

        #region === Embed Guild Encounter Notification ===
        /// <summary>
        /// Builds a compact encounter notification embed for the guild channel,
        /// so other members can see a player has entered a battle.
        /// </summary>
        /// <param name="playerName">The name of the player who encountered the NPC.</param>
        /// <param name="npc">The NPC that was encountered.</param>
        /// <returns>An EmbedBuilder with the encounter notification.</returns>
        public static EmbedBuilder BuildGuildEncounterEmbed(string playerName, NpcModel npc)
        {
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"⚔️ {playerName} encounters a {npc.Name}!")
                .WithThumbnailUrl($"{npc.ThumbHpNpc_100}")
                .WithDescription($"**[{npc.Name!.ToUpper()}]**\n*{npc.Description}*");
        }
        #endregion Embed Guild Encounter Notification

        #region === Embed Guild Flee Notification ===
        /// <summary>
        /// Builds a compact flee notification embed for the guild channel,
        /// so other members can see a player has fled from battle.
        /// </summary>
        /// <param name="playerName">The name of the player who fled.</param>
        /// <param name="npcName">The name of the NPC the player fled from.</param>
        /// <returns>An EmbedBuilder with the flee notification.</returns>
        public static EmbedBuilder BuildGuildFleeEmbed(string playerName, string npcName)
        {
            return new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($"🏃 {playerName} flees from {npcName}!");
        }
        #endregion Embed Guild Flee Notification

        #region === Embed PreBattle ===
        /// <summary>
        /// Displays the pre-battle preparation screen in a private message (DM).
        /// This keeps battle interactions private while keeping the main channel clean.
        /// Sends weapon selection embed to DM with all available weapons and items as buttons.
        /// </summary>
        /// <param name="interaction">The Discord interaction triggered by the player's button click.</param>
        public static async Task EmbedPreBattleInDM(SocketInteraction interaction)
        {
            // --- Retrieve the current battle state for the user ---
            BattleSession session = BattleStateSetup.GetBattleSession(interaction.User.Id);
            if (session == null)
            {
                LogService.Error("[EmbedBuilders.EmbedPreBattleInDM] > Battle state not found.");
                await interaction.RespondAsync("❌ No active battle found.", ephemeral: true);
                return;
            }

            // --- Build the pre-battle UI elements ---
            EmbedBuilder embed = BuildPreBattleEmbed(session);
            ComponentBuilder buttonBuilder = BuildBattleButtons(session);

            // --- Send new message for weapon selection (separate from encounter message) ---
            IUserMessage? dmMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                interaction,
                embed.Build(),
                buttonBuilder.Build());

            if (dmMessage != null)
            {
                BattlePrivateMessageHelper.SetActiveBattleMessage(interaction.User.Id, dmMessage.Id);
                LogService.Info("[EmbedBuilders.EmbedPreBattleInDM] ✅ Weapon selection sent as new DM message.");
            }
            else
            {
                LogService.Error("[EmbedBuilders.EmbedPreBattleInDM] ❌ Failed to send weapon selection message");
            }
        }

        /// <summary>
        /// Builds all the interactive Discord buttons for the pre-battle screen,
        /// including weapon, item, and flee options.
        /// </summary>
        /// <param name="session">The current player's battle session.</param>
        /// <returns>A ComponentBuilder containing all buttons.</returns>
        public static ComponentBuilder BuildBattleButtons(BattleSession session)
        {
            ComponentBuilder buttonBuilder = new ComponentBuilder();

            // --- Add a button for each weapon the player currently owns --- 
            if (session.Context.PlayerWeapons != null && session.Context.PlayerWeapons.Any())
            {
                foreach (WeaponModel weapon in session.Context.PlayerWeapons)
                    buttonBuilder.WithButton(weapon.Name, weapon.Id, ButtonStyle.Primary);
            }

            // --- Add a button for each item (like potions or consumables) --- 
            if (session.Context.Items != null && session.Context.Items.Any())
            {
                foreach (ItemModel item in session.Context.Items)
                    buttonBuilder.WithButton(item.Name, item.Id, ButtonStyle.Success, row: 2);
            }
            else
            {
                LogService.Info("[EmbedBuilders.EmbedPreBattle] > No items available.");
            }

            // --- Add the 'Flee' or 'Break' button to allow exiting the battle --- 
            buttonBuilder.WithButton("Flee", $"battle_flee_{session.Context.Player.Id}", ButtonStyle.Secondary);

            return buttonBuilder;
        }

        /// <summary>
        /// Builds the pre-battle embed containing player stats, equipped weapons, armor, and items.
        /// </summary>
        /// <param name="session">The current player's battle session.</param>
        /// <returns>An EmbedBuilder containing the formatted pre-battle view.</returns>
        public static EmbedBuilder BuildPreBattleEmbed(BattleSession session)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("[Prepare for Battle]")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1425077955121381427/weaponrack2.jpg?ex=68e646c5&is=68e4f545&hm=0f79e4a059c952bda3811786473f42769493eaea55fc71105b2925024727022c&")
                .AddField($"**{session.Context.Player.Name}** prepares for battle...",
                          $"| Level: {session.Context.Player.Level} | XP: {session.Context.Player.XP} | HP: {session.Context.Player.Hitpoints} |");

            // --- Add detailed sections for weapons, armor, and items --- 
            AddWeaponFields(embed, session);
            AddArmorFields(embed, session);
            AddItemFields(embed, session);

            return embed;
        }

        /// <summary>
        /// Adds weapon details to the pre-battle embed, including damage dice and descriptions.
        /// </summary>
        /// <param name="embed">The embed builder to append fields to.</param>
        /// <param name="session">The current player's battle session.</param>
        private static void AddWeaponFields(EmbedBuilder embed, BattleSession session)
        {
            foreach (WeaponModel weapon in session.Context.PlayerWeapons ?? Enumerable.Empty<WeaponModel>())
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
        /// <param name="session">The current player's battle session.</param>
        private static void AddArmorFields(EmbedBuilder embed, BattleSession session)
        {
            foreach (ArmorModel armor in session.Context.PlayerArmor ?? Enumerable.Empty<ArmorModel>())
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
        /// <param name="session">The current player's battle session.</param>
        private static void AddItemFields(EmbedBuilder embed, BattleSession session)
        {
            foreach (ItemModel item in session.Context.Items ?? Enumerable.Empty<ItemModel>())
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
            BattleSession session = BattleStateSetup.GetBattleSession(userId);
            PlayerModel player = session.Context.Player;
            NpcModel npc = session.Context.Npc;

            // --- Check for battle end (player or NPC HP <= 0) ---
            if (player.Hitpoints <= 0 || session.State.CurrentHitpointsNPC <= 0)
            {
                LogService.Info($"[EmbedBuilders.BuildBattleEmbed] Battle ended. Player HP: {player.Hitpoints}, NPC HP: {session.State.CurrentHitpointsNPC}");

                // Return a simple "battle ended" embed; the outer method should call EmbedEndBattle()
                return new EmbedBuilder()
                    .WithTitle("⚰️ Battle Ended")
                    .WithDescription("The fight is over. One side has fallen...");
            }

            // --- Determine NPC thumbnail based on HP percentage ---
            string thumbUrl = HPStatusHelpers.GetNpcThumbnailByHP(npc, session.State.PercentageHpNpc);

            // --- Build the ongoing battle embed ---
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(session.State.EmbedColor)
                .WithTitle($"⚔️ Battle Report — Round {session.State.RoundCounter}")
                .WithThumbnailUrl(thumbUrl)
                .AddField(
                    $"{player.Name} ({session.State.StateOfPlayer}) VS {npc.Name} ({session.State.StateOfNPC})",
                    $"| Level {player.Level} | {player.XP} XP | {player.Hitpoints} HP |")
                .AddField("🩸 Attack Summary", attackSummary, false)
                .WithFooter($"Round {session.State.RoundCounter} completed.");

            return embed;
        }
        #endregion Embed Battle

        #region === Embed Guild Battle Update ===
        /// <summary>
        /// Builds a compact battle update embed for the guild channel,
        /// allowing other members to follow the battle progress.
        /// </summary>
        /// <param name="session">The current battle session.</param>
        /// <param name="attackSummary">The formatted attack summary for the current round.</param>
        /// <returns>An EmbedBuilder with a compact battle summary for the guild channel.</returns>
        public static EmbedBuilder BuildGuildBattleUpdateEmbed(BattleSession session, string attackSummary)
        {
            string thumbUrl = HPStatusHelpers.GetNpcThumbnailByHP(session.Context.Npc, session.State.PercentageHpNpc);

            return new EmbedBuilder()
                .WithColor(session.State.EmbedColor)
                .WithTitle($"⚔️ {session.Context.Player.Name} VS {session.Context.Npc.Name} — Round {session.State.RoundCounter}")
                .WithThumbnailUrl(thumbUrl)
                .AddField("\u200B", attackSummary, false)
                .AddField("[Status]",
                    $"{session.Context.Player.Name}: {session.Context.Player.Hitpoints} HP ({session.State.StateOfPlayer}) | " +
                    $"{session.Context.Npc.Name}: {session.State.StateOfNPC}", false);
        }
        #endregion Embed Guild Battle Update

        #region === Embed End Battle ===
        /// <summary>
        /// Ends the battle and updates the DM message with final results.
        /// Shows final battle stats, XP rewards, and continuation button.
        /// Updates the existing DM message instead of creating a new one.
        /// </summary>
        public static async Task EmbedEndBattleInDM(SocketInteraction interaction, string? extraMessage = null, bool leveledUp = false)
        {
            ulong userId = interaction.User.Id;
            BattleSession session = BattleStateSetup.GetBattleSession(userId);
            string thumbUrl = HPStatusHelpers.GetNpcThumbnailByHP(session.Context.Npc, session.State.PercentageHpNpc);

            string finalLog = extraMessage ?? "";
            string battleOverText = $"{BattleMessages.BattleOver}";

            // --- Check ASI eligibility VOORAF if level up occurred ---
            bool shouldDisableButton = false;
            if (leveledUp)
            {
                PlayerModel? player = SlashCommandHelpers.GetOrCreatePlayer(userId, "");
                shouldDisableButton = SlashCommandHelpers.CheckAbilityScoreEligibility(player);
                LogService.Info($"[EmbedEndBattleInDM] ASI eligibility check: {(shouldDisableButton ? "ELIGIBLE" : "NOT ELIGIBLE")}");
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(session.State.EmbedColor)
                .WithTitle($"⚔️ Battle Report — Round {session.State.RoundCounter}")
                .WithThumbnailUrl(thumbUrl)
                .AddField($"{session.Context.Player.Name} (HP: {session.Context.Player.Hitpoints}) VS {session.Context.Npc.Name} ({session.State.StateOfNPC})",
                          $"| Level {session.Context.Player.Level} | {session.Context.Player.XP} XP | {session.Context.Player.Hitpoints} HP |")
                .AddField("\u200B", $"{finalLog}\n\n{battleOverText}");

            ComponentBuilder buttons = new ComponentBuilder()
                .WithButton("CONTINUE", $"battle_continue_{userId}", ButtonStyle.Success, disabled: shouldDisableButton);

            // --- Send NEW message for end battle (don't update existing) ---
            IUserMessage? newMessage = await BattlePrivateMessageHelper.SendBattleMessageAsync(
                interaction,
                embed.Build(),
                buttons.Build());

            if (newMessage != null)
            {
                BattlePrivateMessageHelper.SetActiveBattleMessage(userId, newMessage.Id);
                LogService.Info("[EmbedEndBattleInDM] ✅ End battle message sent as new DM.");
            }
            else
            {
                LogService.Error("[EmbedEndBattleInDM] ❌ Failed to send end battle message");
            }

            // Reset round counter
            session.State.RoundCounter = 0;

            // Update battle step
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.EndBattle);

            // --- Check if player leveled up and offer Ability Score Improvement ---
            if (leveledUp && shouldDisableButton)
            {
                LogService.Info($"[EmbedEndBattleInDM] Player {userId} leveled up and is eligible for ASI! Sending options...");
                await Task.Delay(1000);
                bool asiSent = await SlashCommandHelpers.SendAbilityScoreImprovementIfEligibleAsync(userId, interaction);
                if (asiSent)
                {
                    LogService.Info($"[EmbedEndBattleInDM] ✅ ASI options sent to player {userId}");
                }
                else
                {
                    LogService.Error($"[EmbedEndBattleInDM] ❌ Failed to send ASI options to player {userId}");
                }
            }
        }
        #endregion Embed End Battle
    }
}