namespace Adventure.Quest.Map
{
    /// <summary>
    /// Responsible for parsing tile position strings.
    /// Handles conversion of "row,col" format to integer coordinates.
    /// </summary>
    public class TilePositionParser
    {
        /// <summary>
        /// Parses a tile position string "row,col" to row/col integers.
        /// Returns (-1, -1) if parsing fails.
        /// </summary>
        public (int row, int col) Parse(string tilePos)
        {
            if (string.IsNullOrWhiteSpace(tilePos))
                return (-1, -1);

            var parts = tilePos.Split(',');
            if (parts.Length != 2)
                return (-1, -1);

            if (int.TryParse(parts[0], out int row) &&
                int.TryParse(parts[1], out int col))
            {
                return (row, col);
            }

            return (-1, -1);
        }
    }
}
