using Adventure.Data;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Battle.TextGenerator;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Discord;

namespace Adventure.Quest.Battle.Attack
{
    /// <summary>
    /// Handles all core attack logic shared between players and NPCs.
    /// Includes hit validation, damage calculation, HP updates, and battle log generation.
    /// </summary>
    public static class AttackProcessor
    {
        #region === Main Processor ===

        /// <summary>
        /// Executes a full attack action (hit check, damage, HP update, log generation).
        /// </summary>
        /// <param name="userId">The player ID participating in battle.</param>
        /// <param name="weapon">The weapon being used for this attack.</param>
        /// <param name="isPlayerAttacker">True if the player is attacking; false if the NPC is attacking.</param>
        /// <returns>
        /// A tuple containing:
        /// - <see cref="string"/> battleLog: formatted text describing the attack result.
        /// - <see cref="BattleState"/> state: updated battle state after the attack.
        /// </returns>
        public static (string battleLog, BattleState state) ProcessAttack(ulong userId, WeaponModel weapon, bool isPlayerAttacker)
        {
            // Determine hit/miss/critical and fetch participants from the active battle
            var (hitResult, state, player, npc, strength) = ValidateAndGetParticipants(userId, isPlayerAttacker);

            // Apply damage based on attack result and attacker type
            ApplyDamage(hitResult, isPlayerAttacker, userId, weapon, strength, state, player, npc);

            // Update HP state descriptions (for embeds and logs)
            UpdateHPStatus(isPlayerAttacker, state, player);

            // Generate a battle log string for Discord display
            string battleLog = GenerateBattleLog(hitResult, isPlayerAttacker, player, npc, weapon, state);

            // Return both the descriptive log and the updated battle state
            return (battleLog, state);
        }

        #endregion

        #region === Hit Validation & Participant Retrieval ===

        /// <summary>
        /// Validates whether the attack hits, misses, or crits,
        /// and retrieves the current participants (player, NPC, and strength values).
        /// </summary>
        private static (ProcessRollsAndDamage.HitResult hitResult, BattleState state, PlayerModel player, NpcModel npc, int strength)
            ValidateAndGetParticipants(ulong userId, bool isPlayerAttacker)
        {
            // Determine hit/miss/critical roll
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker);

            // Retrieve the current battle state and character data
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, isPlayerAttacker);

            return (hitResult, state, player, npc, strength);
        }

        #endregion

        #region === Damage Application ===

        /// <summary>
        /// Applies the appropriate damage if the attack is successful or critical.
        /// Updates the HP of the target accordingly.
        /// </summary>
        private static void ApplyDamage(
            ProcessRollsAndDamage.HitResult hitResult,
            bool isPlayerAttacker,
            ulong userId,
            WeaponModel weapon,
            int strength,
            BattleState state,
            PlayerModel player,
            NpcModel npc)
        {
            // Skip damage if the attack missed or was a critical miss
            if (hitResult != ProcessRollsAndDamage.HitResult.IsValidHit &&
                hitResult != ProcessRollsAndDamage.HitResult.IsCriticalHit)
                return;

            // Attack Player 
            if (isPlayerAttacker)
            {
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
            // Attack NPC
            else
            {
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

        #endregion

        #region === HP Status Update ===

        /// <summary>
        /// Updates HP status information (used for embed display like "Barely Standing", "Wounded", etc.).
        /// </summary>
        private static void UpdateHPStatus(bool isPlayerAttacker, BattleState state, PlayerModel player)
        {
            if (isPlayerAttacker)
            {
                // Update NPC HP state after being attacked
                HPStatusHelpers.GetHPStatus(
                    state.HitpointsAtStartNPC,
                    state.CurrentHitpointsNPC,
                    HPStatusHelpers.TargetType.NPC,
                    state
                );
            }
            else
            {
                // Update Player HP state after being attacked
                HPStatusHelpers.GetHPStatus(
                    state.HitpointsAtStartPlayer,
                    player.Hitpoints,
                    HPStatusHelpers.TargetType.Player,
                    state
                );
            }
        }

        #endregion

        #region === Battle Log ===

        /// <summary>
        /// Builds a descriptive battle log line used for Discord message embeds.
        /// </summary>
        private static string GenerateBattleLog(
            ProcessRollsAndDamage.HitResult hitResult,
            bool isPlayerAttacker,
            PlayerModel player,
            NpcModel npc,
            WeaponModel weapon,
            BattleState state)
        {
            // Convert enum result to string key (e.g., "hit", "miss", "criticalHit")
            string attackResult = GetAttackResult(hitResult);

            // Generate formatted text for the attack log
            return BattleTextGenerator.GenerateBattleLog(
                attackResult,                                                // Attack result category
                isPlayerAttacker ? player.Name! : npc.Name!,                 // Attacker name
                isPlayerAttacker ? npc.Name! : player.Name!,                 // Defender name
                weapon.Name!,                                                // Weapon name
                state.TotalDamage,                                           // Total calculated damage
                isPlayerAttacker ? state.StateOfNPC : state.StateOfPlayer,   // Updated HP state description
                GameData.BattleText!,                                        // Preloaded text templates
                state,                                                       // Current battle state
                GameData.RollText,                                           // Roll result templates
                isPlayerAttacker                                             // Only show dice rolls for players
            );
        }

        #endregion

        #region === Attack Result Conversion ===

        /// <summary>
        /// Converts a <see cref="ProcessRollsAndDamage.HitResult"/> enum
        /// into a string identifier used for text templates.
        /// </summary>
        /// <param name="hitResult">The outcome of the attack roll.</param>
        /// <returns>A string representing the attack category (e.g., "hit", "miss", "criticalHit").</returns>
        public static string GetAttackResult(ProcessRollsAndDamage.HitResult hitResult)
        {
            return hitResult switch
            {
                ProcessRollsAndDamage.HitResult.IsCriticalHit => "criticalHit",
                ProcessRollsAndDamage.HitResult.IsValidHit => "hit",
                ProcessRollsAndDamage.HitResult.IsCriticalMiss => "criticalMiss",
                ProcessRollsAndDamage.HitResult.IsMiss => "miss",
                _ => "hit" // Default fallback in case of unexpected value
            };
        }

        #endregion
    }
}
