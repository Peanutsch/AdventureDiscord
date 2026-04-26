using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Discord;

namespace Adventure.Models.BattleState
{
    /// <summary>
    /// LEGACY: Backward compatibility wrapper for BattleSession.
    /// New code should use BattleSession (Context + State) instead.
    /// This class flattens BattleContext and BattleRuntimeState for existing code.
    /// </summary>
    [Obsolete("Use BattleSession with BattleContext and BattleRuntimeState instead")]
    public class BattleStateModel
    {
        private BattleContext _context = new();
        private BattleRuntimeState _state = new();

        #region === Player / NPC / Weapons / Armor / Items ===
        public PlayerModel Player { get => _context.Player; set => _context.Player = value; }
        public NpcModel Npc { get => _context.Npc; set => _context.Npc = value; }
        public List<WeaponModel> PlayerWeapons { get => _context.PlayerWeapons; set => _context.PlayerWeapons = value; }
        public List<ArmorModel> PlayerArmor { get => _context.PlayerArmor; set => _context.PlayerArmor = value; }

        public List<ItemModel> Items { get => _context.Items; set => _context.Items = value; }

        public List<WeaponModel> NpcWeapons { get => _context.NpcWeapons; set => _context.NpcWeapons = value; }
        public List<ArmorModel> NpcArmor { get => _context.NpcArmor; set => _context.NpcArmor = value; }

        public ArmorModel ArmorElements { get => _context.ArmorElements; set => _context.ArmorElements = value; }
        #endregion

        #region === Roll for NPC Stats ===
        public int DiceCountHP { get => _context.DiceCountHP; set => _context.DiceCountHP = value; }
        public int DiceValueHP { get => _context.DiceValueHP; set => _context.DiceValueHP = value; }
        public string DisplayCR { get => _context.DisplayCR; set => _context.DisplayCR = value; }
        #endregion

        #region === Battle roll tracking ===
        public int CurrentHitpointsNPC { get => _state.CurrentHitpointsNPC; set => _state.CurrentHitpointsNPC = value; }
        public int HitpointsAtStartNPC { get => _state.HitpointsAtStartNPC; set => _state.HitpointsAtStartNPC = value; }

        public int HitpointsAtStartPlayer { get => _state.HitpointsAtStartPlayer; set => _state.HitpointsAtStartPlayer = value; }
        public int PreHpPlayer { get => _state.PreHpPlayer; set => _state.PreHpPlayer = value; }
        public int PreHpNPC { get => _state.PreHpNPC; set => _state.PreHpNPC = value; }
        public int PercentageHpPlayer { get => _state.PercentageHpPlayer; set => _state.PercentageHpPlayer = value; }
        public int PercentageHpNpc { get => _state.PercentageHpNpc; set => _state.PercentageHpNpc = value; }

        public string StateOfNPC { get => _state.StateOfNPC; set => _state.StateOfNPC = value; }
        public string StateOfPlayer { get => _state.StateOfPlayer; set => _state.StateOfPlayer = value; }
        #endregion

        #region === Attack Roll ===
        public int AttackRoll { get => _state.AttackRoll; set => _state.AttackRoll = value; }
        public int AbilityModifier { get => _state.AbilityModifier; set => _state.AbilityModifier = value; }
        public int ProficiencyModifier { get => _state.ProficiencyModifier; set => _state.ProficiencyModifier = value; }
        public bool IsCriticalHit { get => _state.IsCriticalHit; set => _state.IsCriticalHit = value; }
        public bool IsCriticalMiss { get => _state.IsCriticalMiss; set => _state.IsCriticalMiss = value; }
        public string HitResult { get => _state.HitResult; set => _state.HitResult = value; }
        public int TotalAttackRoll { get => _state.TotalAttackRoll; set => _state.TotalAttackRoll = value; }
        #endregion

        #region === Damage + Critical Roll ===
        public int Damage { get => _state.Damage; set => _state.Damage = value; }
        public int CritRoll { get => _state.CritRoll; set => _state.CritRoll = value; }
        public List<int> Rolls { get => _state.Rolls; set => _state.Rolls = value; }
        public string Dice { get => _state.Dice; set => _state.Dice = value; }
        public int TotalDamage { get => _state.TotalDamage; set => _state.TotalDamage = value; }
        #endregion

        #region === Weapon Tracking ===
        public string LastUsedWeapon { get => _state.LastUsedWeapon; set => _state.LastUsedWeapon = value; }
        #endregion

        #region === XP Reward ===
        public int RatioDamageDealt { get => _state.RatioDamageDealt; set => _state.RatioDamageDealt = value; }
        public int RewardXP { get => _state.RewardXP; set => _state.RewardXP = value; }
        public int NewTotalXP { get => _state.NewTotalXP; set => _state.NewTotalXP = value; }
        public bool PlayerLeveledUp { get => _state.PlayerLeveledUp; set => _state.PlayerLeveledUp = value; }
        #endregion

        #region === Guild Channel ===
        /// <summary>
        /// The guild channel ID where the battle was initiated.
        /// Used to send battle updates to the public channel for other members to follow.
        /// </summary>
        public ulong GuildChannelId { get => _state.GuildChannelId; set => _state.GuildChannelId = value; }
        #endregion

        #region === Encounter Location ===
        /// <summary>
        /// The tile ID where the encounter was triggered (e.g., "living_room:2,8").
        /// Used to display encounter marker on the map.
        /// </summary>
        public string EncounterTileId { get => _state.EncounterTileId; set => _state.EncounterTileId = value; }
        #endregion

        #region === Embeds ===
        public Discord.Color EmbedColor { get => _state.EmbedColor; set => _state.EmbedColor = value; }
        public int RoundCounter { get => _state.RoundCounter; set => _state.RoundCounter = value; }

        // Keep track of Discord Message Id
        public Dictionary<int, ulong> RoundMessageIds { get => _state.RoundMessageIds; set => _state.RoundMessageIds = value; }
        #endregion

        #region Conversion Methods

        /// <summary>
        /// Converts this legacy wrapper to the new BattleSession structure.
        /// Useful for gradual migration - call this to use new code paths.
        /// </summary>
        /// <returns>A BattleSession containing the same data.</returns>
        public BattleSession ToSession()
        {
            return new BattleSession
            {
                Context = _context,
                State = _state
            };
        }

        /// <summary>
        /// Creates a BattleStateModel wrapper from a BattleSession.
        /// Useful when new code needs to return data to legacy code.
        /// </summary>
        /// <param name="session">The session to wrap.</param>
        /// <returns>A wrapper that exposes the session data via legacy properties.</returns>
        public static BattleStateModel FromSession(BattleSession session)
        {
            var model = new BattleStateModel
            {
                _context = session.Context,
                _state = session.State
            };
            return model;
        }

        #endregion
    }
}
