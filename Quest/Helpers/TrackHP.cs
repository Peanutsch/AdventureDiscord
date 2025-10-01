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
            if (currentHP <= 0)
                result = "Dead";
            else if (percentHP >= 100)
                result = "Unscathed";
            else if (percentHP >= 90)
                result = "Healthy";
            else if (percentHP >= 80)
                result = "Scratched";
            else if (percentHP >= 70)
                result = "Bruised";
            else if (percentHP >= 60)
                result = "Wounded";
            else if (percentHP >= 50)
                result = "Injured";
            else if (percentHP >= 40)
                result = "Bloodied";
            else if (percentHP >= 30)
                result = "Badly Wounded";
            else // 1–29%
                result = "Grievously Wounded";

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
