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
    public static class AttackProcessor
    {
        /// <summary>
        /// Core shared attack logic for both players and NPCs.
        /// Determines hit/miss, calculates damage, updates HP, and generates a battle log.
        /// </summary>
        public static (string battleLog, BattleStateModel state) ProcessAttack(
            ulong userId,
            WeaponModel weapon,
            bool isPlayerAttacker)
        {
            // Validate hit/miss/crit outcome
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker);

            // Get participants
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, isPlayerAttacker);

            // If a hit, calculate damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit ||
                hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                if (isPlayerAttacker)
                {
                    (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, state.CurrentHitpointsNPC) =
                        ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, state.CurrentHitpointsNPC, true);
                }
                else
                {
                    (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, player.Hitpoints) =
                        ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, false);
                }
            }

            // Update HP status for correct target
            if (isPlayerAttacker)
                TrackHP.GetAndSetHPStatus(state.HitpointsAtStartNPC, state.CurrentHitpointsNPC, TrackHP.TargetType.NPC, state);
            else
                TrackHP.GetAndSetHPStatus(state.HitpointsAtStartPlayer, player.Hitpoints, TrackHP.TargetType.Player, state);

            // Map hit result to attack category
            string attackResult = AttackResultHelper.GetAttackResult(hitResult);

            // Generate battle log text
            string battleLog = BattleTextGenerator.GenerateBattleLog(
                attackResult,
                isPlayerAttacker ? player.Name! : npc.Name!,
                isPlayerAttacker ? npc.Name! : player.Name!,
                weapon.Name!,
                state.TotalDamage,
                isPlayerAttacker ? state.StateOfNPC : state.StateOfPlayer,
                GameData.BattleText!,
                state,
                GameData.RollText,
                strength,
                isPlayerAttacker // Only show dice rolls for player
            );

            return (battleLog, state);
        }
    }
}
