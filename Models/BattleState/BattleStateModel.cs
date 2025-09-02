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
        public CreaturesModel Creatures { get; set; } = new();
        public List<WeaponModel> PlayerWeapons { get; set; } = new();
        public List<ArmorModel> PlayerArmor { get; set; } = new();

        public List<ItemModel> Items { get; set; } = new();

        public List<WeaponModel> CreatureWeapons { get; set; } = new();
        public List<ArmorModel> CreatureArmor { get; set; } = new();

        public ArmorModel ArmorElements { get; set; } = new();

        public bool Hit { get; set; }

        public bool IsCriticalHit { get; set; }

        public bool IsCriticalMiss { get; set; }

        public int AttackRoll { get; set; }

        public int AbilityMod { get; set; }

        public int TotalRoll { get; set; }

        public int Damage { get; set; }
        public string LastUsedWeapon { get; set; } = string.Empty;
        public int PrePlayerHP { get; set; }
        public int PreCreatureHP { get; set; }
    }
}
