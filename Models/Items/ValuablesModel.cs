using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    /// <summary>
    /// Represents a valuable item with intrinsic value.
    /// Inherits shared item properties from BaseItemModel.
    /// </summary>
    public class ValuablesModel : BaseItemModel
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
