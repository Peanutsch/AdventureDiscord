using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
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
    public class EncounterService
    {
        /// <summary>
        /// Randomly selects a npc from the humanoids list.
        /// </summary>
        /// <returns>A random NpcModel instance or null if an error occurs.</returns>
        public static NpcModel? NpcRandomizer()
        {
            try
            {
                // Get the list of humanoid creatures
                var humanoids = GameData.Humanoids;
                var animals = GameData.Animals; 

                var random = new Random();

                // Return a random humanoid creature from the list
                return humanoids![random.Next(humanoids.Count)];
            }
            catch (Exception ex)
            {
                LogService.Error($"[EncounterService.CreatureRandomizer] > Error:\n{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds an embed describing the randomly encountered creature with stats and equipment.
        /// </summary>
        /// <param name="creature">The creature to display in the encounter embed.</param>
        /// <returns>An EmbedBuilder with creature details formatted.</returns>
        public static EmbedBuilder BuildEmbedRandomEncounter(NpcModel npc, BattleStateModel state)
        {
            LogService.DividerParts(1, "Data NPC");

            LogService.Info($"[EncounterService.GetRandomEncounter] > Encountered: [{npc.Name}]");

            //var HitpointsCrFormat = $"HP: {state.HitpointsNPC} ({state.DiceCountHP}d{state.DiceValueHP}) / CR: {ChallengeRatingHelpers.DisplayCR(npc.CR) ({state.DiceCountHP}d{state.DiceValueHP})}";
            var HitpointsCrFormat = $"HP: {state.CurrentHitpointsNPC}";

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⚔️ Encounter")
                .WithDescription($"**[{npc.Name!.ToUpper()}]**\n*\"{npc.Description}\"*");
                //.AddField($"[Hit Points]", HitpointsCrFormat, false);
            //.AddField($"[Hit Points / Challenge Rate]", HitpointsCrFormat, false);

            LogService.Info($"[EncounterService.GetRandomEncounter] > Armor: {string.Join(",", npc.Armor ?? new())}");

            if (npc.Armor?.Any() == true)
            {
                // Retrieve detailed armor info based on armor IDs/names
                var armorList = GameEntityFetcher.RetrieveArmorAttributes(npc.Armor);

                if (armorList.Count > 0)
                {
                    // Add each armor piece as a separate field with details
                    foreach (var armor in armorList)
                    {
                        embed.AddField($"**[{armor.Name}]**\n",
                            $"Type: {armor.Type} armor\n" +
                            //$"Armor Class: {armor.ArmorClass}\n" +
                            $"Weight: {armor.Weight}kg\n" +
                            $"*\"{armor.Description}\"*", false);
                    }
                }
                else
                {
                    LogService.Error("[EncounterService.GetRandomEncounter] > No armor data resolved.");
                    embed.AddField("Armor:", "None", false);
                }
            }

            LogService.Info($"[EncounterService.GetRandomEncounter] > Weapons: {string.Join(",", npc.Weapons ?? new())}");

            if (npc.Weapons?.Any() == true)
            {
                // Retrieve detailed weapon info based on weapon IDs/names
                var weaponList = GameEntityFetcher.RetrieveWeaponAttributes(npc.Weapons!);
                if (weaponList.Count > 0)
                {
                    // Add each weapon as a separate field with details
                    foreach (var weapon in weaponList)
                    {
                        embed.AddField($"**[{weapon.Name}]**",
                            $"Range: {weapon.Range} meter\n" +
                            $"Weight: {weapon.Weight}kg\n" +
                            $"*\"{weapon.Description}\"*", false);
                    }
                }
                else
                {
                    LogService.Error($"[EncounterService.GetRandomEncounter] > weaponList = 0");
                    embed.AddField("Weapons:", "None", false);
                }

                LogService.DividerParts(2, "Data NPC");
            }

            /*
            // Loot field is commented out but could be added in the future
            LogService.Info($"[EncounterService.GetRandomEncounter] > Loot: {string.Join(",", creature.Loot ?? new())}");
            if (creature.Loot?.Any() == true)
                embed.AddField("Loot", string.Join(", ", creature.Loot), false);
            */

            return embed;
        }

        /// <summary>
        /// Rebuilds an embed summarizing the current battle state including HP before attack and attack log.
        /// </summary>
        /// <param name="userId">User ID of the player in battle.</param>
        /// <param name="preHPPlayer">Player's HP before the attack.</param>
        /// <param name="preNpcHP">NPC's HP before the attack.</param>
        /// <param name="attackSummary">Text describing the attack results.</param>
        /// <returns>An EmbedBuilder summarizing the battle.</returns>
        public static EmbedBuilder RebuildBattleEmbed(ulong userId, int preHPPlayer, int preHPNPC, string attackSummary)
        {
            var state = BattleMethods.GetBattleState(userId);
            var player = state.Player;
            var npc = state.Npc;

            // Set current State of NPC
            //state.StateOfNPC = TrackHP.GetAndSetHPStatus(state.HitpointsAtStartNPC, state.CurrentHitpointsNPC, TrackHP.TargetType.NPC, state);
            TrackHP.GetAndSetHPStatus(state.HitpointsAtStartNPC, state.CurrentHitpointsNPC, TrackHP.TargetType.NPC, state);
            LogService.Info($"[EncounterService.RebuildBattleEmbed] {npc.Name} HP at Start: {state.HitpointsAtStartNPC} {npc.Name} current HP: {state.CurrentHitpointsNPC} {npc.Name} State: {state.StateOfNPC}");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{player.Name} ({state.Player.Hitpoints} HP) ⚔️ {npc.Name} ({state.StateOfNPC})") // {state.CurrentHitpointsNPC} HP)") //(CR: {ChallengeRatingHelpers.DisplayCR(npc.CR)})")
                /*
                .AddField("[HP before attack]",
                    $"\n{player.Name}: {preHPPlayer} HP", false) // VS {npc.Name}: {preHPNPC}", false)
                */
                .AddField("[Battle Log]",
                    $"{attackSummary}", false);

            return embed;
        }

        /// <summary>
        /// Shows the player their weapon choices by updating the message with weapon buttons and embed.
        /// </summary>
        /// <param name="component">The component interaction from Discord (button click).</param>
        public static async Task PrepareForBattleChoices(SocketMessageComponent component)
        {
            var state = BattleMethods.GetBattleState(component.User.Id);
            var weapons = state?.PlayerWeapons;
            var items = state?.Items;
            var armors = state?.PlayerArmor;

            var builder = new ComponentBuilder();

            // Create a button for each weapon in the player's inventory
            if (weapons != null)
            {
                foreach (var weapon in weapons)
                {
                    builder.WithButton(weapon.Name, weapon.Id, ButtonStyle.Primary);
                }
            }

            // Create a button for each item in the player's inventory
            if (items!.Count > 0)
            {
                foreach (var item in items)
                {
                    builder.WithButton(item.Name, item.Id, ButtonStyle.Success, row: 2);
                }
            }
            else
            {
                LogService.Info("[EncounterService.ShowWeaponChoices] items == 0");
            }

                var embed = new EmbedBuilder()
                    .WithTitle($"**{state!.Player.Name}** prepares for battle...")
                    .WithColor(Color.DarkRed)
                    .WithDescription($"🔪 Your Inventory:");

            // Add weapons to embed
            foreach (var weapon in weapons!)
            {
                string diceNotation = $"{weapon.Damage.DiceCount}d{weapon.Damage.DiceValue}";
                string nameNotation = $"[{weapon.Name!} ({diceNotation})]";
                embed.AddField(nameNotation, $"*{weapon.Description}*");
            }

            // Add button "Flee" on same row as weapons
            builder.WithButton("Flee!", "btn_flee", ButtonStyle.Secondary);

            // Add armors to embed
            foreach (var armor in armors!)
            {
                string acNotation = $"Armor Class: {armor.ArmorClass}";
                string nameNotation = $"[{armor.Name} ({acNotation})]";
                embed.AddField(nameNotation, $"*{armor.Description}*");
            }

            // Add items to embed
            foreach (var item in items!)
            {
                string diceNotation = $"{item.Effect.DiceCount}d{item.Effect.DiceValue}+{item.Effect.BonusHP}";
                string nameNotation = $"[{item.Name} ( {diceNotation} )]";
                embed.AddField(nameNotation, $"*{item.Description}*");
            }

            // Update the interaction response with new embed and buttons
            await component.UpdateAsync(msg =>
            {
                msg.Content = "";
                msg.Embed = embed.Build();
                msg.Components = builder.Build();
            });
        }
    }
}
