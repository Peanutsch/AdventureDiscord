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
    /// Central repository for all shared game data collections.
    /// 
    /// This static class is loaded once during game initialization and provides 
    /// global access to all core game entities including players, NPCs, items, 
    /// equipment, and map information. All data is maintained in memory for fast access 
    /// throughout the game session.
    /// 
    /// <remarks>
    /// Thread Safety: This class is not thread-safe. Modifications should be synchronized
    /// if accessed from multiple threads.
    /// 
    /// Usage: Access properties statically via GameData.Players, GameData.Weapons, etc.
    /// </remarks>
    /// </summary>
    public static class GameData
    {
        #region === Character Data ===

        /// <summary>Player data collection.</summary>
        private static List<PlayerModel>? _player;

        /// <summary>
        /// Gets or sets the list of all players currently registered in the game.
        /// Each PlayerModel contains character stats, inventory, progression, and save data.
        /// </summary>
        public static List<PlayerModel>? Player
        {
            get => _player;
            set => _player = value;
        }

        #endregion

        #region === NPC Data ===

        /// <summary>Humanoid NPC data collection.</summary>
        private static List<NpcModel>? _humanoids;

        /// <summary>
        /// Gets or sets the list of humanoid NPCs (non-player characters) in the game.
        /// Includes humans, elves, dwarves, and similar character-like entities.
        /// Each NPC contains combat stats, weapons, armor, and loot tables.
        /// </summary>
        public static List<NpcModel>? Humanoids
        {
            get => _humanoids;
            set => _humanoids = value;
        }

        /// <summary>Beast and creature data collection.</summary>
        private static List<NpcModel>? _bestiary;

        /// <summary>
        /// Gets or sets the list of beasts, animals, and non-humanoid creatures in the game.
        /// Includes monsters, magical creatures, and wildlife.
        /// Each entry contains combat statistics, abilities, and potential loot drops.
        /// </summary>
        public static List<NpcModel>? Bestiary
        {
            get => _bestiary;
            set => _bestiary = value;
        }

        #endregion

        #region === Equipment & Items ===

        /// <summary>Weapon data collection.</summary>
        private static List<WeaponModel>? _weapons;

        /// <summary>
        /// Gets or sets the list of all available weapons in the game.
        /// Each weapon defines damage values, range, and combat properties.
        /// Weapons can be equipped by players and NPCs for combat encounters.
        /// </summary>
        public static List<WeaponModel>? Weapons
        {
            get => _weapons;
            set => _weapons = value;
        }

        /// <summary>Armor data collection.</summary>
        private static List<ArmorModel>? _armor;

        /// <summary>
        /// Gets or sets the list of all available armor pieces in the game.
        /// Each armor defines protection level (Armor Class), weight, and type (light/medium/heavy).
        /// Armor can be equipped by players to reduce incoming damage during combat.
        /// </summary>
        public static List<ArmorModel>? Armor
        {
            get => _armor;
            set => _armor = value;
        }

        /// <summary>Potion data collection.</summary>
        private static List<PotionModel>? _potions;

        /// <summary>
        /// Gets or sets the list of potions available in the game.
        /// Includes healing potions, buff potions, and other consumable items with beneficial effects.
        /// Can be used during or outside of combat to restore health or apply temporary bonuses.
        /// </summary>
        public static List<PotionModel>? Potions
        {
            get => _potions;
            set => _potions = value;
        }

        /// <summary>General item data collection.</summary>
        private static List<ItemModel>? _items;

        /// <summary>
        /// Gets or sets the list of general items that can be found, collected, or used in the game.
        /// Includes quest items, crafting materials, consumables, and miscellaneous objects.
        /// Each item may have effects, value, or special properties.
        /// </summary>
        public static List<ItemModel>? Items
        {
            get => _items;
            set => _items = value;
        }

        #endregion

        #region === Player Inventory & Storage ===

        /// <summary>Player inventory data collection.</summary>
        private static List<InventoryModel>? _inventory;

        /// <summary>
        /// Gets or sets the collection of all player inventory instances.
        /// Represents player-owned items, equipment, and resources tracked separately from global item definitions.
        /// This includes quantity information and item assignment to specific players.
        /// </summary>
        public static List<InventoryModel>? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        #endregion

        #region === Text & UI Data ===

        /// <summary>Battle text templates collection.</summary>
        private static BattleTextModel? _battleText;

        /// <summary>
        /// Gets or sets the battle text templates used to generate and display combat actions and outcomes.
        /// Contains predefined text patterns for attack descriptions, damage notifications, status effects, and victory/defeat messages.
        /// Essential for dynamic combat narration and player feedback.
        /// </summary>
        public static BattleTextModel? BattleText
        {
            get => _battleText;
            set => _battleText = value;
        }

        /// <summary>Dice roll text templates collection.</summary>
        private static Dictionary<string, string>? _rollText;

        /// <summary>
        /// Gets or sets a dictionary containing text templates for dice roll messages and outcomes.
        /// Maps roll types or outcomes to formatted text strings for Discord display.
        /// Used to provide varied and engaging feedback for random number generation events.
        /// </summary>
        public static Dictionary<string, string>? RollText
        {
            get => _rollText;
            set => _rollText = value;
        }

        #endregion

        #region === Map & World Data ===

        /// <summary>Test house map tile collection.</summary>
        private static List<TileModel>? _testhouse;

        /// <summary>
        /// Gets or sets the list of all tiles that compose the TestHouse map area.
        /// Each tile represents a navigable location with connections to adjacent tiles.
        /// Contains position data, descriptions, connections, and interactive elements.
        /// </summary>
        public static List<TileModel>? TestHouse
        {
            get => _testhouse;
            set => _testhouse = value;
        }

        #endregion
    }
}
