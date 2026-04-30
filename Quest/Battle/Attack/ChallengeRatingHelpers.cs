using Adventure.Models.NPC;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Rolls;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle.Attack
{
    public class ChallengeRatingHelpers
    {
        #region === Get Data by CR ===
        /// <summary>
        /// Returns a formatted string for a fractional or whole challenge rating.
        /// </summary>
        /// <param name="cr">The challenge rating value.</param>
        /// <returns>A string representation of the challenge rating.</returns>
        public static string DisplayCR(double cr) => cr switch
        {
            0.125 => "1/8",
            0.25 => "1/4",
            0.5 => "1/2",
            _ => cr.ToString()
        };

        /// <summary>
        /// Returns hit dice (dice count, dice value) for a given CR.
        /// Used to calculate NPC hitpoints.
        /// </summary>
        public static (int diceCount, int diceValue) GetHitDie(double cr) => cr switch
        {
            <= 0.125 => (2, 6),
            <= 0.25 => (2, 8),
            <= 0.5 => (2, 8),
            <= 1 => (2, 8),
            <= 2 => (3, 8),
            <= 3 => (4, 10),
            <= 5 => (6, 10),
            <= 10 => (12, 10),
            _ => (20, 12)
        };

        /// <summary>
        /// Returns XP awarded to the player for defeating an NPC of given CR.
        /// </summary>
        public static int GetRewardXP(double cr) => cr switch
        {
            <= 0.125 => 25,
            <= 0.25 => 50,
            <= 0.5 => 100,
            <= 1 => 200,
            <= 2 => 450,
            <= 3 => 700,
            <= 5 => 1800,
            <= 10 => 5900,
            _ => 0
        };
        #endregion

        #region === Get NPC Hitpoints ===
        /// <summary>
        /// Determines the hitpoints for an NPC based on its challenge rating.
        /// Rolls the appropriate dice and stores the results in the battle state.
        /// </summary>
        /// <param name="npc">The NPC for which hitpoints are being rolled.</param>
        /// <param name="cr">The challenge rating of the NPC.</param>
        /// <param name="userId">The ID of the player engaging with the NPC.</param>
        /// <returns>The total hitpoints rolled for the NPC.</returns>
        public static int GetNpcHitpoints(NpcModel npc, double cr, ulong userId)
        {
            (int diceCount, int diceValue) = GetHitDie(cr);

            // Save Dice to BattleState
            var session = BattleStateSetup.GetBattleSession(userId);
            session.Context.Npc = npc;
            session.Context.DiceCountHP = diceCount;
            session.Context.DiceValueHP = diceValue;

            var result = DiceRoller.RollWithoutDetails(diceCount, diceValue);

            LogService.Info($"[ChallengeRatingHelpers.GetNpcHitpoints] Rolled {diceCount}d{diceValue} for NPC HP: {result}");

            return result;
        }
        #endregion
    }
}
