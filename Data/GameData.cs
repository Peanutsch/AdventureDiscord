using Adventure.Loaders;
using Adventure.Models.Inventory;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Models.Text;
using System.Collections;
using System.Collections.Generic;

namespace Adventure.Data
{
    /// <summary>
    /// Static class that holds the game's shared data collections.
    /// Data is loaded once and stored in these properties for global access.
    /// </summary>
    public static class GameData
    {
        private static List<PlayerModel>? _player;

        /// <summary>
        /// Gets or sets the list of players in the game.
        /// </summary>
        public static List<PlayerModel>? Player
        {
            get => _player;
            set => _player = value;
        }

        private static List<NpcModel>? _humanoids;

        /// <summary>
        /// Gets or sets the list of humanoid creatures in the game.
        /// </summary>
        public static List<NpcModel>? Humanoids
        {
            get => _humanoids;
            set => _humanoids = value;
        }

        private static List<NpcModel>? _bestiary;

        /// <summary>
        /// Gets or sets the list of animal creatures in the game.
        /// </summary>
        public static List<NpcModel>? Bestiary 
        {
            get => _bestiary;
            set => _bestiary = value;
        }

        private static List<WeaponModel>? _weapons;

        /// <summary>
        /// Gets or sets the list of weapons available in the game.
        /// </summary>
        public static List<WeaponModel>? Weapons
        {
            get => _weapons;
            set => _weapons = value;
        }

        private static List<ArmorModel>? _armor;

        /// <summary>
        /// Gets or sets the list of armor items available in the game.
        /// </summary>
        public static List<ArmorModel>? Armor
        {
            get => _armor;
            set => _armor = value;
        }

        private static List<PotionModel>? _potions;

        /// <summary>
        /// Gets or sets the list of potions available in the game.
        /// </summary>
        public static List<PotionModel>? Potions
        {
            get => _potions;
            set => _potions = value;
        }

        private static List<ItemModel>? _items;

        public static List<ItemModel>? Items
        {
            get => _items;
            set => _items = value;
        }

        private static List<InventoryModel>? _inventory;

        public static List<InventoryModel>? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        private static BattleTextModel? _battleText;

        public static BattleTextModel? BattleText 
        {
            get => _battleText;
            set => _battleText = value;
        }

        private static Dictionary<string, string>? _rollText;

        public static Dictionary<string, string>? RollText 
        {
            get => _rollText;
            set => _rollText = value;
        }
    }
}
