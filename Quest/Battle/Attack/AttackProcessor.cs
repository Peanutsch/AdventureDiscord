using Adventure.Data;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Battle.TextGenerator;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Discord;

namespace Adventure.Quest.Battle.Attack
{
    /// <summary>
    /// Handles all shared attack logic between players and NPCs.
    /// - Rolls for hit/miss/critical
    /// - Calculates and applies damage
    /// - Updates HP states
    /// - Generates descriptive battle logs for embeds
    /// </summary>
    public static class AttackProcessor
    {
        #region === Processor ===

        /// <summary>
        /// Executes an attack action and processes its outcome.
        /// </summary>
        /// <param name="userId">The ID of the player currently in battle.</param>
        /// <param name="weapon">The weapon model being used in the attack.</param>
        /// <param name="isPlayerAttacker">True if the player is attacking; false if the NPC is attacking.</param>
        /// <returns>
        /// A tuple containing:
        /// - <see cref="string"/> battleLog: a formatted text description of the attack result
        /// - <see cref="BattleState"/> state: the updated battle state after the attack
        /// </returns>
        public static (string battleLog, BattleState state) ProcessAttack(ulong userId, WeaponModel weapon, bool isPlayerAttacker)
        {
            // Determine if the attack hits, misses, or crits
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker);

            // Retrieve current participants (Player, NPC, Strength) from the active battle state
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, isPlayerAttacker);

            // If it's a valid hit or critical hit, calculate and apply damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit ||
                hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                if (isPlayerAttacker)
                {
                    // Player attacks NPC
                    (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, state.CurrentHitpointsNPC) =
                        ProcessSuccesAttack.ProcessSuccessfulHit(
                            userId,
                            state,
                            weapon,
                            strength,
                            state.CurrentHitpointsNPC,
                            true
                        );
                }
                else
                {
                    // NPC attacks player
                    (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, player.Hitpoints) =
                        ProcessSuccesAttack.ProcessSuccessfulHit(
                            userId,
                            state,
                            weapon,
                            strength,
                            player.Hitpoints,
                            false
                        );
                }
            }

            // Update HP status for the correct TargetType (Player or NPC)
            if (isPlayerAttacker)
                HPStatusHelpers.GetHPStatus(
                    state.HitpointsAtStartNPC,
                    state.CurrentHitpointsNPC,
                    HPStatusHelpers.TargetType.NPC,
                    state
                );
            else
                HPStatusHelpers.GetHPStatus(
                    state.HitpointsAtStartPlayer,
                    player.Hitpoints,
                    HPStatusHelpers.TargetType.Player,
                    state
                );

            // Convert hit result into text-based category (used in battle text)
            string attackResult = GetAttackResult(hitResult);

            // Generate dynamic battle log text for display in Discord
            string battleLog = BattleTextGenerator.GenerateBattleLog
            (
                attackResult,                                                // Type of hit (hit/miss/crit)
                isPlayerAttacker ? player.Name! : npc.Name!,                 // Attacker name
                isPlayerAttacker ? npc.Name! : player.Name!,                 // Defender name
                weapon.Name!,                                                // Weapon name
                state.TotalDamage,                                           // Calculated total damage
                isPlayerAttacker ? state.StateOfNPC : state.StateOfPlayer,   // Updated HP state description
                GameData.BattleText!,                                        // Preloaded text templates
                state,                                                       // Current battle state for context
                GameData.RollText,                                           // Roll result templates
                isPlayerAttacker                                             // Include dice rolls in log only for player
            );

            // Return the battle log and the updated state
            return (battleLog, state);
        }

        #endregion

        #region === Get Attack Result ===

        /// <summary>
        /// Converts a <see cref="ProcessRollsAndDamage.HitResult"/> enum into a string identifier used for text templates.
        /// </summary>
        /// <param name="hitResult">The result of the attack roll.</param>
        /// <returns>A string representing the attack type (e.g., "hit", "miss", "criticalHit").</returns>
        public static string GetAttackResult(ProcessRollsAndDamage.HitResult hitResult)
        {
            return hitResult switch
            {
                ProcessRollsAndDamage.HitResult.IsCriticalHit => "criticalHit",
                ProcessRollsAndDamage.HitResult.IsValidHit => "hit",
                ProcessRollsAndDamage.HitResult.IsCriticalMiss => "criticalMiss",
                ProcessRollsAndDamage.HitResult.IsMiss => "miss",
                _ => "hit", // Default fallback in case of unexpected value
            };
        }

        #endregion
    }
}
