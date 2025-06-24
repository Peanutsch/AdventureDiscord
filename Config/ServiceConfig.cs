using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adventure.Gateway;

namespace Adventure.Config
{
    public static class ServiceConfig
    {
        /// <summary>
        /// Configures all necessary services and builds the service provider.
        /// </summary>
        /// <returns>A fully built ServiceProvider with all dependencies registered.</returns>
        public static ServiceProvider Configure()
        {
            var services = new ServiceCollection();

            // Register the DiscordSocketClient with specific gateway intents
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildMessages |
                                 GatewayIntents.DirectMessages |
                                 GatewayIntents.MessageContent
            }));


            // Register the Discord's InteractionService for e.g. slashcommands and buttons)
            services.AddSingleton<InteractionService>(provider =>
            {
                var client = provider.GetRequiredService<DiscordSocketClient>();
                return new InteractionService(client.Rest);
            });

            // Register the AdventureBotGateway
            services.AddSingleton<AdventureBotGateway>(provider =>
            {
                var client = provider.GetRequiredService<DiscordSocketClient>();
                var interactions = provider.GetRequiredService<InteractionService>();
                return new AdventureBotGateway(client, interactions, provider);
            });

            // Register HTTP-based services via AddHttpClient


            // Register helpers that do not depend on HttpClient via AddSingleton


            // Return the fully built service provider
            return services.BuildServiceProvider();
        }
    }
}
