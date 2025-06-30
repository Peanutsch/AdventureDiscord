using Adventure.Data;
using Discord;
using System.Collections.Generic;
using System.Drawing;

public static class InventoryEmbedBuilder
{
    public static EmbedBuilder BuildInventoryEmbed(Dictionary<int, string> inventory)
    {
        var embed = new EmbedBuilder()
            .WithColor(Discord.Color.DarkRed)
            .WithTitle("Your INVENTORY contains:");

        if (inventory.Count == 0)
        {
            embed.WithDescription("Your inventory is empty.");
        }
        else
        {
            foreach (var item in inventory)
            {
                //embed.AddField("\u200b", $"{item.Key}x {item.Value}", inline: false);
                embed.AddField($"----------", $"{item.Key}x {item.Value}", inline: false);
            }
        }

        return embed;
    }

    public static EmbedBuilder BuildTextEncounterEmbed()
    {
        var goblins = GameData.Humanoids;

        var embed = new EmbedBuilder()
            .WithColor(Discord.Color.DarkBlue)
            .WithTitle("Encounter Event");

        embed.AddField("Group", $"2 {goblins}s");

        return embed;
    }
}
