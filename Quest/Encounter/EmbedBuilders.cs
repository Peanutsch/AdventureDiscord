using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Modules;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Helpers;
using Adventure.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Encounter
{
    public class EmbedBuilders
    {
        
        #region Embed Random Encounter
        /// <summary>
        /// Builds an embed describing the randomly encountered creature with stats and equipment.
        /// </summary>
        /// <param name="creature">The creature to display in the encounter embed.</param>
        /// <returns>An EmbedBuilder with creature details formatted.</returns>
        public static EmbedBuilder EmbedRandomEncounter(NpcModel npc)
        {
            LogService.DividerParts(1, "Data NPC");
            LogService.Info($"[EncounterService.GetRandomEncounter] > Encountered: [{npc.Name}]");

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⚔️ Encounter")
                .WithThumbnailUrl($"{npc.ThumbnailNpc}")
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
            LogService.Info($"[EncounterService.AddArmorFields] > Armor: {string.Join(",", npc.Armor ?? new())}");

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
                LogService.Error("[EncounterService.AddArmorFields] > No armor data resolved.");
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
            LogService.Info($"[EncounterService.AddWeaponFields] > Weapons: {string.Join(",", npc.Weapons ?? new())}");

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
                LogService.Error("[EncounterService.AddWeaponFields] > weaponList = 0");
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

        #region Embed PreBattle
        /// <summary>
        /// Displays the pre-battle preparation screen where the player can view their equipment
        /// and select a weapon or item before starting combat.
        /// </summary>
        /// <param name="component">The Discord component triggered by a player's button interaction.</param>
        public static async Task EmbedPreBattle(SocketMessageComponent component)
        {
            // Retrieve the battle state for the current user
            var state = BattleStateSetup.GetBattleState(component.User.Id);
            if (state == null)
            {
                LogService.Error("[EmbedPreBattle] > Battle state not found.");
                return;
            }

            // Build UI buttons for weapons, items, and flee action
            var builder = BuildPreBattleButtons(state);

            // Build the pre-battle embed displaying stats and available equipment
            var embed = BuildPreBattleEmbed(state);

            // Update the original Discord message with the new embed and components
            await component.UpdateAsync(msg =>
            {
                msg.Content = string.Empty;
                msg.Embed = embed.Build();
                msg.Components = builder.Build();
            });
        }

        /// <summary>
        /// Builds all the interactive Discord buttons for the pre-battle screen,
        /// including weapon, item, and flee options.
        /// </summary>
        /// <param name="state">The current player's battle state.</param>
        /// <returns>A ComponentBuilder containing all buttons.</returns>
        private static ComponentBuilder BuildPreBattleButtons(BattleState state)
        {
            var builder = new ComponentBuilder();

            // Add a button for each weapon the player currently owns
            if (state.PlayerWeapons != null && state.PlayerWeapons.Any())
            {
                foreach (var weapon in state.PlayerWeapons)
                    builder.WithButton(weapon.Name, weapon.Id, ButtonStyle.Primary);
            }

            // Add a button for each item (like potions or consumables)
            if (state.Items != null && state.Items.Any())
            {
                foreach (var item in state.Items)
                    builder.WithButton(item.Name, item.Id, ButtonStyle.Success, row: 2);
            }
            else
            {
                LogService.Info("[EmbedPreBattle] > No items available.");
            }

            // Add the 'Flee' or 'Break' button to allow exiting the battle
            builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary);

            return builder;
        }

        /// <summary>
        /// Builds the pre-battle embed containing player stats, equipped weapons, armor, and items.
        /// </summary>
        /// <param name="state">The current player's battle state.</param>
        /// <returns>An EmbedBuilder containing the formatted pre-battle view.</returns>
        private static EmbedBuilder BuildPreBattleEmbed(BattleState state)
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("[Prepare for Battle]")
                .AddField($"**{state.Player.Name}** prepares for battle...",
                          $"| Level: {state.Player.Level} | HP: {state.Player.Hitpoints} | XP: {state.Player.XP} |");

            // Add detailed sections for weapons, armor, and items
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
        private static void AddWeaponFields(EmbedBuilder embed, BattleState state)
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
        private static void AddArmorFields(EmbedBuilder embed, BattleState state)
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
        private static void AddItemFields(EmbedBuilder embed, BattleState state)
        {
            foreach (var item in state.Items ?? Enumerable.Empty<ItemModel>())
            {
                string diceNotation = $"{item.Effect.DiceCount}d{item.Effect.DiceValue}+{item.Effect.BonusHP}";
                string nameNotation = $"[{item.Name} ({diceNotation})]";
                embed.AddField(nameNotation, $"*{item.Description}*");
            }
        }
        #endregion Embed PreBattle

        #region Embed Battle
        /// <summary>
        /// Rebuilds an embed summarizing the current battle state including HP before attack and attack log.
        /// </summary>
        /// <param name="userId">User ID of the player in battle.</param>
        /// <param name="preHPPlayer">Player's HP before the attack.</param>
        /// <param name="preNpcHP">NPC's HP before the attack.</param>
        /// <param name="attackSummary">Text describing the attack results.</param>
        /// <returns>An EmbedBuilder summarizing the battle.</returns>
        public static EmbedBuilder EmbedBattle(ulong userId, string attackSummary)
        {
            var state = BattleStateSetup.GetBattleState(userId);
            var player = state.Player;
            var npc = state.Npc;

            // Set current State of NPC
            TrackHP.GetAndSetHPStatus(state.HitpointsAtStartNPC, state.CurrentHitpointsNPC, TrackHP.TargetType.NPC, state);
            LogService.Info($"[EncounterService.RebuildBattleEmbed]\n\n{npc.Name} HP at Start: {state.HitpointsAtStartNPC} {npc.Name} current HP: {state.CurrentHitpointsNPC} {npc.Name} Health: {state.PercentageHpNpc}% State: {state.StateOfNPC}\n\n");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(state.EmbedColor)
                .WithTitle("[Battle Report]")
                .AddField($"{player.Name} ({state.StateOfPlayer}) VS {npc.Name} ({state.StateOfNPC})", $"| Level: {state.Player.Level} | HP: {state.Player.Hitpoints} | XP: {state.Player.XP} |", inline: true)
                .AddField("\u200B", $"{attackSummary}", false);

            return embed;
        }
        #endregion Embed Battle
    }
}
