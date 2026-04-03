using Adventure.Models.Config;
using Adventure.Services;

namespace Adventure.Loaders
{
    /// <summary>
    /// Loads bot configuration from botconfig.json.
    /// </summary>
    public static class BotConfigLoader
    {
        public static BotConfigModel Load()
        {
            var config = JsonDataManager.LoadObjectFromJson<BotConfigModel>("Data/Config/botconfig.json");
            if (config == null)
            {
                LogService.Error("[BotConfigLoader] Failed to load Data/Config/botconfig.json. Using defaults.");
                return new BotConfigModel();
            }

            LogService.Info($"[BotConfigLoader] BotConfig loaded. GuildChannelId: {config.GuildChannelId}");
            return config;
        }
    }
}
