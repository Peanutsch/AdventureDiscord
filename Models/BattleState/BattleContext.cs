using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;

namespace Adventure.Models.BattleState
{
    /// <summary>
    /// Domain model representing the entities and equipment involved in a battle.
    /// Contains the core game entities but no runtime state or UI concerns.
    /// </summary>
    public class BattleContext
    {
        #region Entities

        public PlayerModel Player { get; set; } = new();
        public NpcModel Npc { get; set; } = new();

        #endregion

        #region Equipment & Items

        public List<WeaponModel> PlayerWeapons { get; set; } = new();
        public List<ArmorModel> PlayerArmor { get; set; } = new();
        public List<ItemModel> Items { get; set; } = new();

        public List<WeaponModel> NpcWeapons { get; set; } = new();
        public List<ArmorModel> NpcArmor { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();

        #endregion

        #region NPC Stats

        public int DiceCountHP { get; set; }
        public int DiceValueHP { get; set; }
        public string DisplayCR { get; set; } = "UNKNOWN";

        #endregion
    }
}
