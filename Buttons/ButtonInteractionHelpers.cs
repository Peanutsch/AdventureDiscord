// Adventure/Buttons/ButtonInteractionHelpers.cs
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Adventure.Buttons
{
    public static class ButtonInteractionHelpers
    {
        public static async Task RemoveButtonsAsync(SocketMessageComponent component, string message)
        //public static async Task RemoveButtonsAsync(SocketMessageComponent component)
        {
            // Haal bestaande embed op
            var originalEmbed = component.Message.Embeds.FirstOrDefault()?.ToEmbedBuilder()?.Build();

            // Maak een nieuwe embed aan met de actie
            var resultEmbed = new EmbedBuilder()
                .WithDescription($"🗡️ {message}")
                .WithColor(Color.DarkGreen)
                .Build();

            await component.UpdateAsync(msg =>
            {
                msg.Embeds = originalEmbed != null
                    ? new[] { originalEmbed, resultEmbed }
                    : new[] { resultEmbed };

                msg.Components = new ComponentBuilder().Build(); 
                msg.Content = ""; 
            });
        }

        public static async Task RemoveButtonsAsync(SocketMessageComponent component)
        {
            await component.UpdateAsync(msg =>
            {
                msg.Components = new ComponentBuilder().Build();
            });
        }
    }
}
