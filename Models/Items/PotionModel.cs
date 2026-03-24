using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    /// <summary>
    /// Represents a potion item with point value.
    /// Inherits shared item properties from BaseItemModel.
    /// </summary>
    public class PotionModel : BaseItemModel
    {
        [JsonPropertyName("points")]
        public int Points { get; set; }
    }
}
