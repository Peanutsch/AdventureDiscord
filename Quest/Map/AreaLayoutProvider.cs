using Adventure.Loaders;
using Adventure.Models.Map;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Responsible for retrieving and managing area layout data.
    /// Encapsulates data access logic for area lookups.
    /// </summary>
    public class AreaLayoutProvider
    {
        /// <summary>
        /// Attempts to retrieve the area and its layout for a given tile.
        /// Returns false if area is missing or layout is invalid.
        /// </summary>
        public bool TryGetAreaLayout(TileModel tile, out TestHouseAreaModel? area, out List<List<string>> layout)
        {
            layout = new();
            
            if (!TestHouseLoader.AreaLookup.TryGetValue(tile.AreaId, out area))
                return false;

            if (area?.Layout == null || area.Layout.Count == 0)
                return false;

            layout = area.Layout;
            return true;
        }
    }
}
