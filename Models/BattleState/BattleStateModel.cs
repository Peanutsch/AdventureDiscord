using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;

namespace Adventure.Models.BattleState
{
    public class BattleStateModel
    {
        public PlayerModel Player { get; set; } = new();
        public NpcModel Npc { get; set; } = new();
        public List<WeaponModel> PlayerWeapons { get; set; } = new();
        public List<ArmorModel> PlayerArmor { get; set; } = new();

        public List<ItemModel> Items { get; set; } = new();

        public List<WeaponModel> NpcWeapons { get; set; } = new();
        public List<ArmorModel> NpcArmor { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();

        // Roll NPC Stats
        public int DiceCountHP { get; set; }
        public int DiceValueHP { get; set; }
        public string DisplayCR { get; set; } = "UNKNOWN";

        // Battle roll tracking
        public int CurrentHitpointsNPC { get; set; }
        public int HitpointsAtStartNPC { get; set; }
        public int PreHpPlayer { get; set; }
        public int PreHpNPC { get; set; }
        public int PercentageHpPlayer { get; set; }
        public int PercentageHpNpc { get; set; }
        //public int PlayerLevelAtStart { get; set; }

        public string StateOfNPC { get; set; } = "UNKNOWN";
        public string StateOfPlayer { get; set; } = "UNKNOWN";


        // Battle roll attack
        public int AttackRoll { get; set; }
        public int AbilityModifier { get; set; }
        public int ProficiencyModifier { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public int TotalRoll { get; set; }

        public int CritRoll { get; set; }
        public int Damage { get; set; }
        public List<int> Rolls { get; set; } = new();
        public string Dice { get; set; } = "UNKNOWN";
        public int TotalDamage { get; set; }

        // Weapon Tracking
        public string LastUsedWeapon { get; set; } = "UNKNOWN";

        // XP Reward
        public int RewardXP { get; set; }
        public int NewTotalXP { get; set; }
    }
}
