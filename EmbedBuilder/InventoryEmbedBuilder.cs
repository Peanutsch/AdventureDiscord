using Discord;
using System.Collections.Generic;
using System.Drawing;

public static class InventoryEmbedBuilder
{
    public static EmbedBuilder BuildInventoryEmbed(Dictionary<string, int> inventory)
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
                embed.AddField("\u200b", $"{item.Key}: {item.Value}x", inline: false);
            }
        }

        return embed;
    }
}
