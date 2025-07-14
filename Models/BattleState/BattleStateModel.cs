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
        public List<WeaponModel> CreatureWeapons { get; set; } = new();
        public List<ArmorModel> CreatureArmor { get; set; } = new();
    }
}
