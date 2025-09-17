using Adventure.Models.BattleState;
using Adventure.Services;
using System;

namespace Adventure.Quest.Helpers
{
    public class TrackHP
    {
        /// <summary>
        /// Specifies the target entity type (Player or NPC).
        /// </summary>
        public enum TargetType
        {
            Player,
            NPC
        }

        /// <summary>
        /// Determines the HP status of a target (Player or NPC) based on starting HP and current HP.
        /// Updates the corresponding state in the provided <see cref="BattleStateModel"/>.
        /// </summary>
        /// <param name="startHP">The starting HP value of the target.</param>
        /// <param name="currentHP">The current HP value of the target.</param>
        /// <param name="target">The target type (Player or NPC).</param>
        /// <param name="battleState">The battle state model to update with HP status and percentage.</param>
        public static void GetAndSetHPStatus(int startHP, int currentHP, TargetType target, BattleStateModel battleState)
        {
            string result;

            // If starting HP is invalid (0 or less), mark the state as unknown
            if (startHP <= 0)
            {
                if (target == TargetType.Player)
                    battleState.StateOfPlayer = "UNKNOWN: startHP <= 0";
                else
                    battleState.StateOfNPC = "UNKNOWN: startHP <= 0";

                return;
            }

            // Calculate HP percentage
            double percentHP = (double)currentHP / startHP * 100;
            var roundedPercentHP = (int)Math.Round(percentHP);

            // Determine status based on HP percentage
            if (percentHP > 50 && percentHP <= 75)
                result = "Lightly Wounded";
            else if (percentHP > 25 && percentHP <= 50)
                result = "Heavily Wounded";
            else if (percentHP > 0 && percentHP <= 25)
                result = "Deadly Wounded";
            else if (percentHP <= 0)
                result = "💀 DEAD";
            else
                result = "Healthy";

            // Update the battle state depending on target type
            if (target == TargetType.Player)
            {
                battleState.StateOfPlayer = result;
            }
            else
            {
                battleState.StateOfNPC = result;
                battleState.PercentageHpNpc = roundedPercentHP;
            }
        }
    }
}
