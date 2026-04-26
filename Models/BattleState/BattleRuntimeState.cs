using Discord;

namespace Adventure.Models.BattleState
{
    /// <summary>
    /// Service-level state model for tracking runtime battle information.
    /// Contains transient state like rolls, damage tracking, HP changes, and UI state.
    /// </summary>
    public class BattleRuntimeState
    {
        #region Battle Tracking

        public int CurrentHitpointsNPC { get; set; }
        public int HitpointsAtStartNPC { get; set; }
        public int HitpointsAtStartPlayer { get; set; } = 100;

        public int PreHpPlayer { get; set; }
        public int PreHpNPC { get; set; }
        public int PercentageHpPlayer { get; set; }
        public int PercentageHpNpc { get; set; }

        public string StateOfNPC { get; set; } = "UNKNOWN";
        public string StateOfPlayer { get; set; } = "UNKNOWN";

        #endregion

        #region Attack Roll

        public int AttackRoll { get; set; }
        public int AbilityModifier { get; set; }
        public int ProficiencyModifier { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public string HitResult { get; set; } = "UNKNOWN";
        public int TotalAttackRoll { get; set; }

        #endregion

        #region Damage & Critical Roll

        public int Damage { get; set; }
        public int CritRoll { get; set; }
        public List<int> Rolls { get; set; } = new();
        public string Dice { get; set; } = "UNKNOWN";
        public int TotalDamage { get; set; }

        #endregion

        #region Weapon Tracking

        public string LastUsedWeapon { get; set; } = "UNKNOWN";

        #endregion

        #region XP Reward

        public int RatioDamageDealt { get; set; }
        public int RewardXP { get; set; }
        public int NewTotalXP { get; set; }
        public bool PlayerLeveledUp { get; set; } = false;

        #endregion

        #region Location & Channel

        /// <summary>
        /// The guild channel ID where the battle was initiated.
        /// Used to send battle updates to the public channel for other members to follow.
        /// </summary>
        public ulong GuildChannelId { get; set; }

        /// <summary>
        /// The tile ID where the encounter was triggered (e.g., "living_room:2,8").
        /// Used to display encounter marker on the map.
        /// </summary>
        public string EncounterTileId { get; set; } = string.Empty;

        #endregion

        #region UI State

        public Color EmbedColor { get; set; } = Color.Red;
        public int RoundCounter { get; set; }

        /// <summary>
        /// Tracks Discord message IDs per round for editing/updating messages.
        /// </summary>
        public Dictionary<int, ulong> RoundMessageIds { get; set; } = new();

        #endregion
    }
}
