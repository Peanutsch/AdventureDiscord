using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Discord.Commands;

namespace Adventure
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Set up the DI container
            var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>(sp => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.GuildMessages |
                                     GatewayIntents.DirectMessages |
                                     GatewayIntents.MessageContent
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionService>(sp => new InteractionService(sp.GetRequiredService<DiscordSocketClient>().Rest)) // Correctly inject the DiscordSocketClient's Rest client
                .AddSingleton<CancellationTokenSource>()
                .AddSingleton<AdventureBot>()
                .BuildServiceProvider();

            // Get the AdventureBot instance from the DI container
            var bot = services.GetRequiredService<AdventureBot>();

            // Start the bot
            await bot.StartAsync();
        }
    }
}
