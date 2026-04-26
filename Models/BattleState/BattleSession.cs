namespace Adventure.Models.BattleState
{
    /// <summary>
    /// Combined battle session containing both domain context and runtime state.
    /// This is what's stored in-memory during active battles.
    /// </summary>
    public class BattleSession
    {
        /// <summary>
        /// Domain model: entities and equipment involved in the battle.
        /// </summary>
        public BattleContext Context { get; set; } = new();

        /// <summary>
        /// Service state: runtime tracking, rolls, damage, and UI state.
        /// </summary>
        public BattleRuntimeState State { get; set; } = new();
    }
}
