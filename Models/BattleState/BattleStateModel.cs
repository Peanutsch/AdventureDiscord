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
        public required PlayerModel Player { get; set; }
        public required CreaturesModel Creatures { get; set; }
        public required List<WeaponModel> PlayerWeapons { get; set; }
        public required List<ArmorModel> PlayerArmor { get; set; }
        public required List<WeaponModel> CreatureWeapons { get; set; }
        public required List<ArmorModel> CreatureArmor { get; set; }
    }
}
