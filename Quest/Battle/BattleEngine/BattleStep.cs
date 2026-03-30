namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Enumeration of all possible battle phases in the encounter system.
    /// 
    /// Each step represents a distinct phase in the battle state machine:
    /// - Start: Initial encounter, player chooses action (attack/flee)
    /// - Flee: Player has fled the battle
    /// - WeaponChoice: Player is selecting their weapon
    /// - Battle: Active combat phase with attacks resolving
    /// - PostBattle: After attacks complete, evaluating end conditions
    /// - EndBattle: Battle has concluded, cleanup phase
    /// </summary>
    public enum BattleStep
    {
        /// <summary>Initial battle setup, player chooses to attack or flee.</summary>
        Start,

        /// <summary>Player has fled from the battle.</summary>
        Flee,

        /// <summary>Player is selecting their weapon for combat.</summary>
        WeaponChoice,

        /// <summary>Active combat phase - attacks are being resolved.</summary>
        Battle,

        /// <summary>Post-attack phase - evaluating battle end conditions.</summary>
        PostBattle,

        /// <summary>Battle has ended - cleanup and exit phase.</summary>
        EndBattle
    }
}
