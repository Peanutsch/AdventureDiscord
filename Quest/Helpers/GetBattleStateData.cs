using Adventure.Models.BattleState;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Battle.BattleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Helpers
{
    class GetBattleStateData
    {
        #region GET DATA
        /// <summary>
        /// Retrieves the current battle state, player, creature, and attacker's strength.
        /// </summary>
        /// <param name="userId">The Discord user ID involved in the battle.</param>
        /// <param name="playerIsAttacker">True if the player is the attacker, false if the creature is.</param>
        /// <returns>A tuple containing the battle state, player model, creature model, and attacker strength.</returns>
        public static (BattleStateModel state, PlayerModel player, NpcModel creature, int attackerStrength) GetBattleParticipants(ulong userId, bool playerIsAttacker)
        {
            var state = BattleMethods.GetBattleState(userId);
            var player = state.Player;
            var npc = state.Npc;

            // Determine the strength value of the attacker (player or creature)
            int attackerStrength = playerIsAttacker ? player.Attributes.Strength : npc.Attributes.Strength;

            return (state, player, npc, attackerStrength);
        }
        #endregion GET DATA
    }
}
