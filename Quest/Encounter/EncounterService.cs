using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Creatures;
using Adventure.Models.Player;
using Adventure.Modules;
using Adventure.Quest.Battle;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Encounter
{
    public class EncounterService
    {
        public static CreaturesModel? CreatureRandomizer()
        {
            try
            {
                //List shuffle = (humanoids_shuffle, animals_shuffle);

                var humanoids = GameData.Humanoids;
                var animals = GameData.Animals;

                var random = new Random();

                //var rndNPC = random.Next([humanoids, animals]);

                //var pickedNPC = rndNPC![random.Next(rndNPC.Count)];
                
                //return humanoids![random.Next(humanoids.Count)];
                return humanoids![random.Next(humanoids.Count)];
            }
            catch (Exception ex)
            {
                LogService.Error($"[EncounterService.CreatureRandomizer] > Error:\n{ex.Message}");

                return null;
            }
        }


        public static EmbedBuilder BuildEmbedRandomEncounter(CreaturesModel creature)
        {
            LogService.DividerParts(1, "Data NPC");

            LogService.Info($"[EncounterService.GetRandomEncounter] > Encountered: [{creature.Name}]");
            ;

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⚔️ Encounter")
                .WithDescription($"**[{creature.Name!.ToUpper()}]** appears!\n*\"{creature.Description}\"*")
                .AddField("Hit Points:", creature.Hitpoints, false);

            LogService.Info($"[EncounterService.GetRandomEncounter] > Armor: {string.Join(",", creature.Armor ?? new())}");
            if (creature.Armor?.Any() == true)
            {
                var armorList = GameEntityFetcher.RetrieveArmorAttributes(creature.Armor);

                if (armorList.Count > 0)
                {
                    foreach (var armor in armorList)
                    {
                        embed.AddField($"**[{armor.Name}]**\n",
                            $"Type: {armor.Type} armor\n" +
                            $"AC Bonus: +{armor.AC_Bonus}\n" +
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

            LogService.Info($"[EncounterService.GetRandomEncounter] > Weapons: {string.Join(",", creature.Weapons ?? new())}");
            if (creature.Weapons?.Any() == true)
            {
                var weaponList = GameEntityFetcher.RetrieveWeaponAttributes(creature.Weapons!);
                if (weaponList.Count > 0)
                {
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
            LogService.Info($"[EncounterService.GetRandomEncounter] > Loot: {string.Join(",", creature.Loot ?? new())}");
            if (creature.Loot?.Any() == true)
                embed.AddField("Loot", string.Join(", ", creature.Loot), false);
            */

            return embed;
        }

        public static EmbedBuilder RebuildBattleEmbed(ulong userId, string weaponName, int damage, int prePlayerHP, int preNpcHP)
        {
            var state = BattleEngine.GetBattleState(userId);
            var player = state.Player;
            var npc = state.Creatures;

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{player.Name} ⚔️ {npc.Name}")
                .AddField("⚔️ Battle Summary",
                    $"**HP before attack**\nPlayer: {prePlayerHP}\nCreature: {preNpcHP}", false)
                .AddField("🗡️ Attack Log",
                    $"You attacked {npc.Name} with your {weaponName} for {damage} damage.\n" +
                    $"Your Hitpoints: {player.Hitpoints}\n" +
                    $"{npc.Name} Hitpoints: {npc.Hitpoints}", false)
                .AddField("🧭 Choose your next action!", "Attack or Flee", false);

            return embed;
        }
    }
}
