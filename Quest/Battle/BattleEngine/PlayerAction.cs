namespace Adventure.Quest.Battle.BattleEngine
{
    /// <summary>
    /// Enumeration of player actions that can be performed during battles.
    /// 
    /// These actions represent the discrete choices a player can make:
    /// - Flee: Attempt to escape from the current battle
    /// - Attack: Engage in combat with the enemy
    /// </summary>
    public enum PlayerAction
    {
        /// <summary>Player attempts to flee from the battle.</summary>
        Flee,

        /// <summary>Player chooses to attack the enemy.</summary>
        Attack
    }
}
