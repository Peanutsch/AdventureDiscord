using Adventure.Loaders;
using Adventure.Models.Creatures;
using Adventure.Models.Items;
using Adventure.Models.Player;

namespace Adventure.Data
{
    public static class GameData
    {
        public static List<WeaponModel> Weapons => WeaponLoader.Load();
        public static List<ArmorModel> Armor => ArmorLoader.Load();
        public static List<PotionModel> Potions => PotionLoader.Load();
        public static List<CreaturesModel> Humanoids => HumanoidLoader.Load();
        public static List<PlayerModel> Player => PlayerLoader.Load();
    }
}
