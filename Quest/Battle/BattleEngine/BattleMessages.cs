namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Centralized collection of predefined messages used throughout the battle system.
    /// 
    /// All player-facing UI messages are stored here for:
    /// - Consistency across the application
    /// - Easy localization/translation in the future
    /// - Single source of truth for message content
    /// </summary>
    public static class BattleMessages
    {
        /// <summary>Message displayed when player successfully flees from battle.</summary>
        public const string Flee = "You fled. The forest grows quiet.";

        /// <summary>Prompt shown when player needs to choose their weapon.</summary>
        public const string ChooseWeapon = "Choose your weapon:";

        /// <summary>Message displayed when a battle concludes.</summary>
        public const string BattleOver = "Battle is over!";

        /// <summary>Fallback message when an action cannot be processed.</summary>
        public const string NothingHappens = "Nothing happens...";
    }
}
