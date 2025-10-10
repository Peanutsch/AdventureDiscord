using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Discord;

namespace Adventure.Models.BattleState
{
    public class BattleState
    {
        #region === Player / NPC / Weapons / Armor / Items ===
        public PlayerModel Player { get; set; } = new();
        public NpcModel Npc { get; set; } = new();
        public List<WeaponModel> PlayerWeapons { get; set; } = new();
        public List<ArmorModel> PlayerArmor { get; set; } = new();

        public List<ItemModel> Items { get; set; } = new();

        public List<WeaponModel> NpcWeapons { get; set; } = new();
        public List<ArmorModel> NpcArmor { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();
        #endregion

        #region === Roll for NPC Stats ===
        public int DiceCountHP { get; set; }
        public int DiceValueHP { get; set; }
        public string DisplayCR { get; set; } = "UNKNOWN";
        #endregion

        #region === Battle roll tracking ===
        public int CurrentHitpointsNPC { get; set; }
        public int HitpointsAtStartNPC { get; set; }

        public int HitpointsAtStartPlayer = 1000;
        public int PreHpPlayer { get; set; }
        public int PreHpNPC { get; set; }
        public int PercentageHpPlayer { get; set; }
        public int PercentageHpNpc { get; set; }

        public string StateOfNPC { get; set; } = "UNKNOWN";
        public string StateOfPlayer { get; set; } = "UNKNOWN";
        #endregion

        #region === Attack Roll ===
        public int AttackRoll { get; set; }
        public int AbilityModifier { get; set; }
        public int ProficiencyModifier { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public string HitResult { get; set; } = "UNKNOWN";
        public int TotalAttackRoll { get; set; }
        #endregion

        #region === Damage + Critical Roll ===
        public int Damage { get; set; }
        public int CritRoll { get; set; }
        public List<int> Rolls { get; set; } = new();
        public string Dice { get; set; } = "UNKNOWN";
        public int TotalDamage { get; set; }
        #endregion

        #region === Weapon Tracking ===
        public string LastUsedWeapon { get; set; } = "UNKNOWN";
        #endregion

        #region === XP Reward ===
        public int RewardXP { get; set; }
        public int NewTotalXP { get; set; }
        #endregion

        #region === Embeds ===
        public Discord.Color EmbedColor { get; set; } = Color.Red;
        public int RoundCounter { get; set; }
        public int NextRound { get; set; }

        // Keep track of Discord Message Id
        public Dictionary<int, ulong> RoundMessageIds { get; set; } = new();
        #endregion
    }
}
