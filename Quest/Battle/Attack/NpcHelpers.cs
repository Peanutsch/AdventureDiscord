using Adventure.Models.NPC;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Rolls;
using Adventure.Services;

namespace Adventure.Quest.Battle.Attack
{
    public class NpcHelpers
    {
        #region === HITPOINTS ===
        /// <summary>
        /// Determines the hitpoints for an NPC based on its challenge rating.
        /// Rolls the appropriate dice and stores the results in the battle state.
        /// </summary>
        /// <param name="npc">The NPC for which hitpoints are being rolled.</param>
        /// <param name="cr">The challenge rating of the NPC.</param>
        /// <param name="userId">The ID of the player engaging with the NPC.</param>
        /// <returns>The total hitpoints rolled for the NPC.</returns>
        public static int GetNpcHitpoints(NpcModel npc, double cr, ulong userId) {
            (int diceCount, int diceValue) = GetHitDie(cr);

            // Save Dice to BattleState
            var state = BattleStateSetup.GetBattleState(userId);
            state.Npc = npc;
            state.DiceCountHP = diceCount;
            state.DiceValueHP = diceValue;

            var result = DiceRoller.RollWithoutDetails(diceCount, diceValue);

            LogService.Info($"[NpcHitpoints.GetNpcHitpoints] Rolled {diceCount}d{diceValue} for NPC {npc.Name} HP: {result}");

            return result;
        }
        #endregion HITPOINTS

        #region === Challenge Rating ===
        /// <summary>
        /// Returns a formatted string for a fractional or whole challenge rating.
        /// </summary>
        /// <param name="cr">The challenge rating value.</param>
        /// <returns>A string representation of the challenge rating.</returns>
        public static string DisplayCR(double cr)
        {
            if (cr == 0.125) return "1/8";
            else if (cr == 0.25) return "1/4";
            else if (cr == 0.5) return "1/2";
            else return cr.ToString();
        }

        /// <summary>
        /// Returns hit dice (dice count, dice value) for a given CR.
        /// Used to calculate NPC hitpoints.
        /// </summary>
        public static (int diceCount, int diceValue) GetHitDie(double cr)
        {
            if (cr <= 0.125) return (2, 6);    
            else if (cr <= 0.25) return (2, 8);   
            else if (cr <= 0.5) return (2, 8);    
            else if (cr <= 1) return (2, 8);    
            else if (cr <= 2) return (3, 8);    
            else if (cr <= 3) return (4, 10);   
            else if (cr <= 5) return (6, 10);   
            else if (cr <= 10) return (12, 10);
            else return (20, 12);
        }

        /// <summary>
        /// Returns XP awarded to the player for defeating an NPC of given CR.
        /// </summary>
        public static int GetRewardXP(double cr)
        {
            if (cr <= 0.125) return 25;
            else if (cr <= 0.25) return 50;
            else if (cr <= 0.5) return 100;
            else if (cr <= 1) return 200;
            else if (cr <= 2) return 450;
            else if (cr <= 3) return 700;
            else if (cr <= 5)  return 1800;
            else if (cr <= 10) return 5900;
            else return 0;
        }
        #endregion CR
    }
}
