using Adventure.Models.Map;
using System.Text;

namespace Adventure.Quest.Map
{
    /// <summary>
    /// Responsible for rendering a visual grid representation of a tile area.
    /// Encapsulates grid rendering logic.
    /// </summary>
    public class GridRenderer
    {
        private readonly TileIconProvider _iconProvider;

        public GridRenderer(TileIconProvider iconProvider)
        {
            _iconProvider = iconProvider ?? throw new ArgumentNullException(nameof(iconProvider));
        }

        /// <summary>
        /// Renders a visual grid for the given area with the player positioned at the specified coordinates.
        /// </summary>
        public string RenderGrid(TestHouseAreaModel area, List<List<string>> layout, int playerRow, int playerCol)
        {
            var sb = new StringBuilder();
            
            for (int row = 0; row < layout.Count; row++)
            {
                for (int col = 0; col < layout[row].Count; col++)
                {
                    string icon = _iconProvider.GetTileIcon(area, row, col, playerRow, playerCol);
                    sb.Append(icon);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
