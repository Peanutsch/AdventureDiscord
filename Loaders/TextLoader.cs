using Adventure.Models.Text;
using Adventure.Services;

namespace Adventure.Loaders
{
    public static class TextLoader
    {
        private static TextContainer? _cached;

        /// <summary>
        /// Returns the TextContainer from JSON.
        /// Loads it only once and caches it for subsequent calls.
        /// </summary>
        public static TextContainer LoadContainer()
        {
            if (_cached != null) return _cached;

            _cached = JsonDataManager.LoadObjectFromJson<TextContainer>("Data/Text/battletext.json");
            if (_cached == null)
            {
                LogService.Error("[TextLoader] Failed to load battle texts. Returning empty container.");
                _cached = new TextContainer(); // fallback naar lege container
            }

            return _cached;
        }

        /// <summary>
        /// Forces reload of the JSON file (optional, for hot-reload during runtime)
        /// </summary>
        public static void Reload()
        {
            _cached = null;
            LoadContainer();
        }
    }
}
