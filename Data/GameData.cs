using Adventure.Loaders;
using Adventure.Models;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;

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

        public static List<NpcModel>? _animals;

        /// <summary>
        /// Gets or sets the list of animal creatures in the game.
        /// </summary>
        public static List<NpcModel>? Animals
        {
            get => _animals;
            set => _animals = value;
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

        public static List<ItemModel>? _items;

        public static List<ItemModel>? Items
        {
            get => _items;
            set => _items = value;
        }

        public static List<InventoryModel>? _inventory;

        public static List<InventoryModel>? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }
    }
}
