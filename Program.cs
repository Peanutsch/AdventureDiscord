namespace Adventure
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var bot = new AdventureBot();
            await bot.StartAsync();
        }
    }
}