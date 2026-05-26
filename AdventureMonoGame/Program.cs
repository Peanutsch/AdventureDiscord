using System;

namespace AdventureMonoGame;

class Program
{
    [System.STAThread]
    static void Main()
    {
        LogService.Info("Starting Adventure MonoGame Client...");

        try
        {
            using (var game = new Game1())
            {
                game.Run();
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"Fatal error: {ex}");
            throw;
        }
    }
}
