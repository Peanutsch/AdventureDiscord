using Adventure.Models.Text;
using Adventure.Services;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Adventure.Loaders 
{
    public static class BattleTextLoader 
    {
        /// <summary>
        /// Load battle flavor text and roll templates from JSON.
        /// </summary>
        /// <param name="battleTextPath">Path to battletext.json</param>
        /// <param name="rollTextPath">Path to battlerolldicetext.json</param>
        /// <returns>Tuple of BattleTextModel and roll-text dictionary</returns>
        public static (BattleTextModel battleText, Dictionary<string, string> rollText) Load() 
        {
            // Flavor text
            var battleText = JsonDataManager.LoadObjectFromJson<BattleTextModel>("Data/Text/battletext.json");
            if (battleText == null)
                throw new System.Exception($"Failed to load Data/Text/battletext.json...");

            // Roll text
            var rollText = JsonDataManager.LoadObjectFromJson<Dictionary<string, string>>("Data/Text/battlerolldicetext.json");
            if (rollText == null)
                throw new System.Exception($"Failed to load Data/Text/battlerolldicetext.json...");

            return (battleText, rollText);
        }
    }
}
