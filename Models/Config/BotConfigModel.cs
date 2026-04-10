using System.Text.Json.Serialization;

namespace Adventure.Models.Config
{
    /// <summary>
    /// Configuration model for bot-wide settings loaded from botconfig.json.
    /// </summary>
    public class BotConfigModel
    {
        /// <summary>
        /// The default guild channel ID where battle updates are sent for other members to follow.
        /// </summary>
        [JsonPropertyName("guildChannelId")]
        public ulong GuildChannelId { get; set; }
    }
}
