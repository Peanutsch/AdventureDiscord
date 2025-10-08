using Adventure.Models.BattleState;
using Adventure.Models.NPC;
using Adventure.Services;
using System;

namespace Adventure.Quest.Helpers
{
    /// <summary>
    /// Helper class for calculating and updating the HP status of players and NPCs during battles.
    /// </summary>
    public class HPStatusHelpers
    {
        #region === TargetType ===
        /// <summary>
        /// Specifies the type of entity whose HP is being tracked.
        /// </summary>
        public enum TargetType
        {
            Player,
            NPC
        }
        #endregion TargetType

        #region === Get HP Status ===
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

            // Calculate HP as a percentage of the starting HP
            double percentHP = (double)currentHP / startHP * 100;

            // Determine textual status based on HP percentage
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

            // Update the battle state with the calculated status
            SetStatus(percentHP, target, result, battleState);
        }
        #endregion Get HP Status

        #region === Process HP Status ===
        /// <summary>
        /// Updates the battle state with the calculated HP status and percentage.
        /// </summary>
        /// <param name="percentHP">The HP percentage of the target.</param>
        /// <param name="target">The target type (Player or NPC).</param>
        /// <param name="result">The textual status (e.g., "Wounded").</param>
        /// <param name="battleState">The battle state to update.</param>
        public static void SetStatus(double percentHP, TargetType target, string result, BattleState battleState)
        {
            int roundedPercentHP = (int)Math.Round(percentHP);

            if (target == TargetType.Player)
            {
                // Update the player’s status
                battleState.StateOfPlayer = result;
            }
            else
            {
                // Update the NPC’s status and percentage HP
                battleState.StateOfNPC = result;
                battleState.PercentageHpNpc = roundedPercentHP;
            }
        }

        /// <summary>
        /// Returns the appropriate NPC thumbnail based on its current HP percentage.
        /// </summary>
        /// <param name="npc">The NPC model.</param>
        /// <param name="percentHP">The NPC's current HP as a percentage.</param>
        /// <returns>The URL of the NPC thumbnail corresponding to its HP level.</returns>
        public static string GetNpcThumbnailByHP(NpcModel npc, int percentHP)
        {
            return percentHP switch
            {
                >= 90 => npc.ThumbnailNpc_100,
                >= 50 => npc.ThumbnailNpc_50,
                >= 10 => npc.ThumbnailNpc_10,
                0 => npc.ThumbnailNpc_0,
                _ => npc.ThumbnailNpc_100 // fallback for unexpected values
            };
        }
        #endregion Process HP Status
    }
}
