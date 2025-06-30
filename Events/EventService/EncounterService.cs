using Adventure.Data;
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
                var humanoids = GameData.Humanoids;
                var random = new Random();
                
                return humanoids[random.Next(humanoids.Count)];
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
                .WithColor(Color.Orange)
                .WithTitle("⚔️ Encounter")
                .WithDescription($"**[{creature.Name!.ToUpper()}]** appears!\n{creature.Description}")
                //.AddField("-----", $"[{creature.Name!.ToUpper()}] appears!\n{creature.Description}", false)
                //.AddField("Description", creature.Description, false)
                .AddField("Hit Points", creature.Hitpoints, false);

            if (creature.Armor?.Any() == true)
                embed.AddField("Armor", string.Join(", ", creature.Armor), false);

            if (creature.Weapons?.Any() == true)
                embed.AddField("Weapons", string.Join(", ", creature.Weapons), false);

            if (creature.Loot?.Any() == true)
                embed.AddField("Loot", string.Join(", ", creature.Loot), false);

            return embed;
        }
    }
}
