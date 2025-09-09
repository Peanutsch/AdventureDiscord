using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Rolls
{
    public class DiceRoller
    {
        private static readonly Random rng = new();

        /// <summary>
        /// Rolls the given number of dice with the specified number of sides.
        /// Returns both the total and the list of individual rolls.
        /// </summary>
        /// <param name="diceCount">How many dice to roll (e.g. 2 for 2d6)</param>
        /// <param name="diceValue">How many sides each die has (e.g. 6 for d6)</param>
        /// <returns>Tuple of (Total, List of Rolls)</returns>
        public static (int Total, List<int> Rolls) RollWithDetails(int diceCount, int diceValue)
        {
            if (diceCount <= 0 || diceValue <= 0)
                throw new ArgumentException("Dice count and value must be greater than 0.");

            var rolls = new List<int>();
            for (int i = 0; i < diceCount; i++)
            {
                var roll = rng.Next(1, diceValue + 1);
                rolls.Add(roll);

                LogService.Info($"[DiceRoller.RollWithDetails] Rolling {roll}");
            }

            return (rolls.Sum(), rolls);
        }

        public static int RollWithoutDetails(int diceCount, int diceValue)
        {
            if (diceCount <= 0 || diceValue <= 0)
                throw new ArgumentException("Dice count and value must be greater than 0.");

            var rolls = new List<int>();
            for (int i = 0; i < diceCount; i++)
            {
                var roll = rng.Next(1, diceValue + 1);
                rolls.Add(roll);
                LogService.Info($"[DiceRoller.RollWithoutDetails] Rolling {roll}");
            }

            return rolls.Sum();
        }
    }
}
