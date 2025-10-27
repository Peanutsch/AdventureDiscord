using Adventure.Loaders;
using Adventure.Models.Inventory;
using Adventure.Models.Items;
using Adventure.Models.Map;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Models.Text;
using System.Collections;
using System.Collections.Generic;

namespace Adventure.Data
{
    /// <summary>
    /// Static class that holds all shared game data collections.
    /// This data is loaded once during initialization and provides global access 
    /// to all core game entities, items, NPCs, and map information.
    /// </summary>
    public static class GameData
    {
        #region === PlayerModel ===
        private static List<PlayerModel>? _player;

        /// <summary>
        /// Gets or sets the list of players currently registered in the game.
        /// </summary>
        public static List<PlayerModel>? Player
        {
            get => _player;
            set => _player = value;
        }
        #endregion

        #region === NpcModel ===
        private static List<NpcModel>? _humanoids;

        /// <summary>
        /// Gets or sets the list of humanoid NPCs (non-player characters) in the game.
        /// </summary>
        public static List<NpcModel>? Humanoids
        {
            get => _humanoids;
            set => _humanoids = value;
        }

        private static List<NpcModel>? _bestiary;

        /// <summary>
        /// Gets or sets the list of beasts, animals, and non-humanoid creatures in the game.
        /// </summary>
        public static List<NpcModel>? Bestiary
        {
            get => _bestiary;
            set => _bestiary = value;
        }
        #endregion

        #region === WeaponModel ===
        private static List<WeaponModel>? _weapons;

        /// <summary>
        /// Gets or sets the list of all available weapons that can be used or equipped by players.
        /// </summary>
        public static List<WeaponModel>? Weapons
        {
            get => _weapons;
            set => _weapons = value;
        }
        #endregion

        #region === ArmorModel ===
        private static List<ArmorModel>? _armor;

        /// <summary>
        /// Gets or sets the list of all available armor pieces that can be equipped by players.
        /// </summary>
        public static List<ArmorModel>? Armor
        {
            get => _armor;
            set => _armor = value;
        }
        #endregion

        #region === PotionModel ===
        private static List<PotionModel>? _potions;

        /// <summary>
        /// Gets or sets the list of potions available in the game, including healing and buff items.
        /// </summary>
        public static List<PotionModel>? Potions
        {
            get => _potions;
            set => _potions = value;
        }
        #endregion

        #region === ItemModel ===
        private static List<ItemModel>? _items;

        /// <summary>
        /// Gets or sets the list of general items that can be found, collected, or used in the game.
        /// </summary>
        public static List<ItemModel>? Items
        {
            get => _items;
            set => _items = value;
        }
        #endregion

        #region === InventoryModel ===
        private static List<InventoryModel>? _inventory;

        /// <summary>
        /// Gets or sets the collection of inventory data, representing player-owned items.
        /// </summary>
        public static List<InventoryModel>? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }
        #endregion

        #region === BattleTextModel ===
        private static BattleTextModel? _battleText;

        /// <summary>
        /// Gets or sets the battle text templates used to display combat actions and outcomes.
        /// </summary>
        public static BattleTextModel? BattleText
        {
            get => _battleText;
            set => _battleText = value;
        }
        #endregion

        #region === Dictionary RollText ===
        private static Dictionary<string, string>? _rollText;

        /// <summary>
        /// Gets or sets a dictionary containing text templates for dice roll messages or outcomes.
        /// </summary>
        public static Dictionary<string, string>? RollText
        {
            get => _rollText;
            set => _rollText = value;
        }
        #endregion

        #region === MainHouseModel ===
        private static List<TileModel>? _mainHouse;

        /// <summary>
        /// Gets or sets the current map configuration, including tiles, connections, and POIs.
        /// </summary>
        public static List<TileModel>? MainHouse
        {
            get => _mainHouse;
            set => _mainHouse = value;
        }
        #endregion

        #region === TestHouseModel ===
        private static List<TileModel>? _testhouse;

        public static List<TileModel>? TestHouse
        {
            get => _testhouse;
            set => _testhouse = value;
        }
        #endregion
    }
}
