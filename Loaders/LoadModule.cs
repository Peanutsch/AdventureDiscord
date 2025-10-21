using Adventure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Loaders
{
    public class LoadModule
    {
        public static void Load()
        {
            // Load static game data into memory at startup
            GameData.Weapons = WeaponLoader.Load();
            GameData.Armor = ArmorLoader.Load();
            GameData.Items = ItemLoader.Load();
            GameData.Humanoids = HumanoidLoader.Load();
            GameData.Bestiary = BestiaryLoader.Load();

            // Load battle and roll text data
            var (battleText, rollText) = BattleTextLoader.Load();
            GameData.BattleText = battleText;
            GameData.RollText = rollText;

            // Load Map data
            GameData.MainHouse = MainHouseLoader.Load();
            GameData.TestHouse = TestHouseLoader.Load();
        }
    }
}
