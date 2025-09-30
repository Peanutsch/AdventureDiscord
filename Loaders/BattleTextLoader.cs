using Adventure.Models.Text;
using Adventure.Services;

namespace Adventure.Loaders {
    public static class BattleTextLoader {
        /// <summary>
        /// Loads the battletext.json and returns a BattleTextModel instance.
        /// </summary>
        /// <returns>BattleTextModel or null if loading fails</returns>
        public static BattleTextModel? Load() {
            try {
                var battleText = JsonDataManager.LoadObjectFromJson<BattleTextModel>("Data/Text/battletext.json");

                if (battleText == null) {
                    LogService.Error("[BattleTextLoader] > Failed to load battletext.json");
                    return null;
                }

                LogService.Info("[BattleTextLoader] > Successfully loaded battletext.json");
                return battleText;
            }
            catch (System.Exception ex) {
                LogService.Error($"[BattleTextLoader] > Error loading battletext.json: {ex.Message}");
                return null;
            }
        }
    }
}

