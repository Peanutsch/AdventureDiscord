using Adventure.Models.Creatures;
using Adventure.Models.Items;
using Adventure.Models.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Battle roll states
        public bool Hit { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }

        public int AttackRoll { get; set; }
        public int AbilityModifier { get; set; }
        public int ProficiencyModifier { get; set; }
        public int TotalRoll { get; set; }

        public int CritRoll { get; set; }
        public int Damage { get; set; }
        public List<int> Rolls { get; set; } = new();
        public string Dice { get; set; } = "";
        public int TotalDamage { get; set; }

        // HP Tracking
        public int PrePlayerHP { get; set; }
        public int PreNpcHP { get; set; }

        public string LastUsedWeapon { get; set; } = string.Empty;
    }
}
