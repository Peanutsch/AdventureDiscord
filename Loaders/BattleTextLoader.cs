using Adventure.Models.Text;
using Adventure.Services;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Adventure.Loaders {
    public static class BattleTextLoader {
        /// <summary>
        /// Laadt battle flavor text en roll templates uit JSON.
        /// </summary>
        /// <param name="battleTextPath">Pad naar battletext.json</param>
        /// <param name="rollTextPath">Pad naar battlerolldicetext.json</param>
        /// <returns>Tuple van BattleTextModel en roll-text dictionary</returns>
        public static (BattleTextModel battleText, Dictionary<string, string> rollText) Load() 
        {
            // 1️⃣ Flavor text
            var battleText = JsonDataManager.LoadObjectFromJson<BattleTextModel>("Data/Text/battletext.json");
            if (battleText == null)
                throw new System.Exception($"Failed to load Data/Text/battletext.json...");

            // 2️⃣ Roll text
            var rollText = JsonDataManager.LoadObjectFromJson<Dictionary<string, string>>("Data/Text/battlerolldicetext.json");
            if (rollText == null)
                throw new System.Exception($"Failed to load Data/Text/battlerolldicetext.json...");

            return (battleText, rollText);
        }
    }
}
