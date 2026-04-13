namespace Adventure.Models.Player
{
    /// <summary>
    /// Represents the current state of a player in the game.
    /// 
    /// States:
    /// - Idle: Player is not in adventure or battle
    /// - InAdventure: Player is exploring the map
    /// - InBattle: Player is actively battling an NPC
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// Player is not doing anything (can start /adventure).
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Player is currently exploring the map.
        /// </summary>
        InAdventure = 1,

        /// <summary>
        /// Player is actively battling an NPC.
        /// </summary>
        InBattle = 2
    }
}
