using Adventure.Models.BattleState;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle.BattleEngine
{
    class GetBattleStateData
    {
        #region GET DATA
        /// <summary>
        /// Retrieves the current battle session, player, creature, and attacker's strength.
        /// </summary>
        /// <param name="userId">The Discord user ID involved in the battle.</param>
        /// <param name="playerIsAttacker">True if the player is the attacker, false if the creature is.</param>
        /// <returns>A tuple containing the battle session, player model, creature model, and attacker strength.</returns>
        public static (BattleSession session, PlayerModel player, NpcModel creature, int attackerStrength) GetBattleParticipants(ulong userId, bool playerIsAttacker)
        {
            var session = BattleStateSetup.GetBattleSession(userId);
            var player = session.Context.Player;
            var npc = session.Context.Npc;

            // Determine the strength value of the attacker (player or creature)
            int attackerStrength = playerIsAttacker ? player.Attributes.Strength : npc.Attributes.Strength;

            return (session, player, npc, attackerStrength);
        }
        #endregion GET DATA
    }
}
