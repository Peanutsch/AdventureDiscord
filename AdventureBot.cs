using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Discord.Commands;

namespace Adventure
{
    public class AdventureBot
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly InteractionService _interactions;

        public AdventureBot()
        {
            // Initialiseer de client en services
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildMessages |
                                 GatewayIntents.DirectMessages |
                                 GatewayIntents.MessageContent
            });

            _commands = new CommandService();
            _interactions = new InteractionService(_client.Rest);
        }

        public async Task StartAsync()
        {
            // De client en services zijn al geïnitialiseerd in de constructor,
            // dus geen herinitialisatie hier nodig.

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;

            // Login en start de bot
            await _client.LoginAsync(TokenType.Bot, GetToken());
            await _client.StartAsync();

            // Voeg modules toe
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            await Task.Delay(-1);  // Houd de applicatie open
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        private static Task ReadyAsync()
        {
            Console.WriteLine("Bot is ready!");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Haalt het Discord bot-token op uit het 'get_token.csv' bestand in de projectroot.
        /// </summary>
        private string GetToken()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "get_token.csv");

            // Log de paden voor debugging
            Debug.WriteLine($"filePath: {filePath}");

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath).Trim();
            }
            else
            {
                throw new FileNotFoundException("Token bestand niet gevonden.", filePath);
            }
        }
    }
}
