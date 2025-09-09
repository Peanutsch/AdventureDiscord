using Adventure.Models.NPC;
using Adventure.Quest.Battle;
using Adventure.Quest.Rolls;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.NpcHelpers
{
    class NpcHitpoints
    {
        public static int GetNpcHitpoints(NpcModel npc, double cr, ulong userId)
        {
            (int diceCount, int diceValue) = HitDiceCR.GetHitDieByCR(cr);

            // Save Dice to BattleState
            var state = BattleEngine.GetBattleState(userId);
            state.Npc = npc;
            state.DiceCountHP = diceCount;
            state.DiceValueHP = diceValue;

            var result = DiceRoller.RollWithoutDetails(diceCount, diceValue);

            LogService.Info($"[NpcHitpoints.GetNpcHitpoints] Rolled {diceCount}d{diceValue} for NPC HP: {result}");

            return result;
        }
    }
}
