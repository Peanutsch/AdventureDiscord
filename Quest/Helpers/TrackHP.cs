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
        /// Updates the corresponding state in the provided <see cref="BattleState"/>.
        /// </summary>
        /// <param name="startHP">The starting HP value of the target.</param>
        /// <param name="currentHP">The current HP value of the target.</param>
        /// <param name="target">The target type (Player or NPC).</param>
        /// <param name="battleState">The battle state model to update with HP status and percentage.</param>
        public static void GetHPStatus(int startHP, int currentHP, TargetType target, BattleState battleState)
        {
            // If starting HP is invalid (0 or less), mark the state as unknown
            if (startHP <= 0)
            {
                if (target == TargetType.Player)
                    battleState.StateOfPlayer = "UNKNOWN: Player's startHP <= 0";
                else
                    battleState.StateOfNPC = "UNKNOWN: NPC's startHP <= 0";

                return;
            }

            // Calculate HP percentage
            double percentHP = (double)currentHP / startHP * 100;

            // Determine status based on HP percentage
            string result = currentHP <= 0 ? "Defeated" : percentHP switch
            {
                >= 100 => "Unscathed",
                >= 90 => "Healthy",
                >= 80 => "Scratched",
                >= 70 => "Bruised",
                >= 60 => "Wounded",
                >= 50 => "Injured",
                >= 40 => "Bloodied",
                >= 30 => "Badly Wounded",
                _ => "Grievously Wounded"
            };

            SetStatus(percentHP, target, result, battleState);
        }

        public static void SetStatus(double percentHP, TargetType target, string result, BattleState battleState)
        {
            int roundedPercentHP = (int)Math.Round(percentHP);

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
