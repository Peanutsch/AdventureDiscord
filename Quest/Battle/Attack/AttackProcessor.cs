using Adventure.Data;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Battle.TextGenerator;
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
        /// - <see cref="BattleSession"/> session: updated battle session after the attack.
        /// </returns>
        public static (string battleLog, BattleSession session) ProcessAttack(ulong userId, WeaponModel weapon, bool isPlayerAttacker)
        {
            // Determine hit/miss/critical and fetch participants from the active battle
            var (hitResult, session, player, npc, strength) = ValidateAndGetParticipants(userId, isPlayerAttacker);

            // Apply damage based on attack result and attacker type
            ApplyDamage(hitResult, isPlayerAttacker, userId, weapon, session, player);

            // Update HP state descriptions (for embeds and logs)
            UpdateHPStatus(isPlayerAttacker, session, player);

            // Generate a battle log string for Discord display
            string battleLog = GenerateBattleLog(hitResult, isPlayerAttacker, player, npc, weapon, session);

            // Return both the descriptive log and the updated battle session
            return (battleLog, session);
        }

        #endregion

        #region === Hit Validation & Participant Retrieval ===

        /// <summary>
        /// Validates whether the attack hits, misses, or crits,
        /// and retrieves the current participants (player, NPC, and strength values).
        /// </summary>
        private static (ProcessRollsAndDamage.HitResult hitResult, BattleSession session, PlayerModel player, NpcModel npc, int strength)
            ValidateAndGetParticipants(ulong userId, bool isPlayerAttacker)
        {
            // Determine hit/miss/critical roll
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker);

            // Retrieve the current battle session and character data
            var (session, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, isPlayerAttacker);

            return (hitResult, session, player, npc, strength);
        }

        #endregion

        #region === Damage Application ===

        /// <summary>
        /// Applies the appropriate damage if the attack is successful or critical.
        /// Updates the HP of the target accordingly.
        /// </summary>
        private static void ApplyDamage(ProcessRollsAndDamage.HitResult hitResult, bool isPlayerAttacker, ulong userId, WeaponModel weapon, BattleSession session, PlayerModel player)
        {
            // Skip damage if the attack missed or was a critical miss
            if (hitResult != ProcessRollsAndDamage.HitResult.IsValidHit &&
                hitResult != ProcessRollsAndDamage.HitResult.IsCriticalHit)
                return;

            // Attack Player 
            if (isPlayerAttacker)
            {
                (session.State.Damage, session.State.TotalDamage, session.State.Rolls, session.State.CritRoll, session.State.Dice, session.State.CurrentHitpointsNPC) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(
                        userId,
                        session,
                        weapon,
                        session.State.CurrentHitpointsNPC,
                        true
                    );
            }
            // Attack NPC
            else
            {
                (session.State.Damage, session.State.TotalDamage, session.State.Rolls, session.State.CritRoll, session.State.Dice, player.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(
                        userId,
                        session,
                        weapon,
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
        private static void UpdateHPStatus(bool isPlayerAttacker, BattleSession session, PlayerModel player)
        {
            if (isPlayerAttacker)
            {
                // Update NPC HP state after being attacked
                HPStatusHelpers.GetHPStatus(
                    session.State.HitpointsAtStartNPC,
                    session.State.CurrentHitpointsNPC,
                    HPStatusHelpers.TargetType.NPC,
                    session
                );
            }
            else
            {
                // Update Player HP state after being attacked
                HPStatusHelpers.GetHPStatus(
                    session.State.HitpointsAtStartPlayer,
                    player.Hitpoints,
                    HPStatusHelpers.TargetType.Player,
                    session
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
            BattleSession session)
        {
            // Convert enum result to string key (e.g., "hit", "miss", "criticalHit")
            string attackResult = GetAttackResult(hitResult);

            // Generate formatted text for the attack log
            return BattleTextGenerator.GenerateBattleLog(
                attackResult,                                                // Attack result category
                isPlayerAttacker ? player.Name! : npc.Name!,                 // Attacker name
                isPlayerAttacker ? npc.Name! : player.Name!,                 // Defender name
                weapon.Name!,                                                // Weapon name
                session.State.TotalDamage,                                   // Total calculated damage
                isPlayerAttacker ? session.State.StateOfNPC : session.State.StateOfPlayer,   // Updated HP state description
                GameData.BattleText!,                                        // Preloaded text templates
                session,                                                     // Current battle session
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
