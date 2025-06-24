using Microsoft.Extensions.DependencyInjection;
using Adventure.Gateway;
using Adventure.Config;

namespace Adventure
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var services = ServiceConfig.Configure();

            var bot = services.GetRequiredService<AdventureBotGateway>();

            await bot.StartBotAsync();

        }
    }
}