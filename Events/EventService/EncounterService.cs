using Adventure.Data;
using Adventure.Helpers;
using Adventure.Models.Creatures;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Events.EventService
{
    public class EncounterService
    {
        public static CreaturesModel? CreatureRandomizer()
        {
            try
            {
                List shuffle = (humanoids_shuffle, animals_shuffle);

                var humanoids = GameData.Humanoids;
                var animals = GameData.Animals;

                var random = new Random();

                var rndNPC = random.Next([humanoids, animals]);

                var pickedNPC = rndNPC![random.Next(rndNPC.Count)];
                
                //return humanoids![random.Next(humanoids.Count)];
                return humanoids![random.Next(humanoids.Count)];
            }
            catch (Exception ex)
            {
                LogService.Error($"[EncounterService.CreatureRandomizer]                 > Error:\n{ex.Message}");

                return null;
            }
        }


        public static EmbedBuilder GetRandomEncounter(CreaturesModel creature)
        {
            LogService.Info($"[EncounterService.GetRandomEncounter] > Encountered: {creature.Name}");

            var embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("⚔️ Encounter")
                .WithDescription($"**[{creature.Name!.ToUpper()}]** appears!\n{creature.Description}")
                .AddField("Hit Points", creature.Hitpoints, false);

            LogService.Info($"[EncounterService.GetRandomEncounter] > Armor: {string.Join(",", creature.Armor ?? new())}");
            if (creature.Armor?.Any() == true)
            {
                var armorList = EntityResolver.ResolveArmorNames(creature.Armor);

                if (armorList.Count > 0)
                {
                    embed.AddField("Armor:", string.Join(", ", armorList), false);
                }
                else
                {
                    LogService.Error($"[EncounterService.GetRandomEncounter] > armorList = 0");
                    embed.AddField("Armor:", "None", false);
                }
            }

            LogService.Info($"[EncounterService.GetRandomEncounter] > Weapons: {string.Join(",", creature.Weapons ?? new())}");
            if (creature.Weapons?.Any() == true)
            {
                var weaponList = EntityResolver.ResolveWeaponNames(creature.Weapons!);
                if (weaponList.Count > 0)
                {
                    embed.AddField("Weapons:", string.Join(", ", weaponList), false);
                }
                else
                {
                    LogService.Error($"[EncounterService.GetRandomEncounter] > weaponList = 0");
                    embed.AddField("Weapons:", "None", false);
                }
                
            }

            /*
            LogService.Info($"[EncounterService.GetRandomEncounter] > Loot: {string.Join(",", creature.Loot ?? new())}");
            if (creature.Loot?.Any() == true)
                embed.AddField("Loot", string.Join(", ", creature.Loot), false);
            */

            return embed;
        }
    }
}
