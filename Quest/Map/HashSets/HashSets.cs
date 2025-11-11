using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map.HashSets
{
    public class HashSets
    {
        // Tiles that cannot be walked on
        public static readonly HashSet<string> NonPassableTiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Wall", "TREASURE", "Water", "Lava", "Trap", "BLOCKt"
        };
    }
}
