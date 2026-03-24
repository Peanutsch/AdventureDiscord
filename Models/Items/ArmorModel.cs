using System.Text.Json.Serialization;

namespace Adventure.Models.Items
{
    /// <summary>
    /// Represents armor item with armor class and type properties.
    /// Inherits shared item properties from BaseItemModel.
    /// </summary>
    public class ArmorModel : BaseItemModel
    {
        [JsonPropertyName("armor_class")]
        public int ArmorClass { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "light"; // light, medium, heavy
    }

    /// <summary>
    /// Container for different types of armor collections.
    /// </summary>
    public class ArmorContainer 
    {
        [JsonPropertyName("crafted_armor")]
        public List<ArmorModel>? CraftedArmor { get; set; }

        [JsonPropertyName("natural_armor")]
        public List<ArmorModel>? NaturalArmor { get; set; }
    }
}
