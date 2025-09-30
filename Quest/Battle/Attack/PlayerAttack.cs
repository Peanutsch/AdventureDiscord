using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Adventure.Services;
using Discord;
using System.Numerics;

namespace Adventure.Quest.Battle.Attack
{
    class PlayerAttack 
    {
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon) {
            // 1️⃣ Determine hit result
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: true);

            // 2️⃣ Get battle participants and state
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: true);

            // 3️⃣ If hit or critical, calculate damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit ||
                hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit) {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, state.CurrentHitpointsNPC) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(
                        userId,
                        state,
                        weapon,
                        strength,
                        state.CurrentHitpointsNPC,
                        isPlayerAttacker: true
                    );
            }

            // 4️⃣ Update NPC HP status
            TrackHP.GetAndSetHPStatus(state.HitpointsAtStartNPC, state.CurrentHitpointsNPC, TrackHP.TargetType.NPC, state);
            string statusLabel = state.StateOfNPC;

            // 5️⃣ Bepaal attackResult via helper
            string attackResult = AttackResultHelper.GetAttackResult(hitResult);

            // 6️⃣ Generate battle log
            string battleLog = BattleTextGenerator.GenerateBattleLog(
                attackResult: attackResult,
                attacker: player.Name!,
                defender: npc.Name!,
                weapon: weapon.Name!,
                damage: state.TotalDamage,
                statusLabel: statusLabel,
                battleText: GameData.BattleText!,
                state: state,
                rollText: GameData.RollText,
                strength: strength,
                isPlayerAttack: true
            );

            // 7️⃣ Handle defeated NPC
            if (state.CurrentHitpointsNPC <= 0) {
                BattleMethods.SetStep(userId, BattleMethods.StepEndBattle);

                var rewardXP = ChallengeRatingHelpers.GetRewardXP(state.Npc.CR);
                var (leveledUp, oldLevel, newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);

                battleLog += $"\n\n💀 **{npc.Name} is defeated!**";
                battleLog += $"\n🏆 **{player.Name}** gains **{state.RewardXP} XP** (Total: {state.NewTotalXP} XP)";

                if (leveledUp)
                    battleLog += $"\n\n✨ **LEVEL UP!** {player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
            }
            else {
                BattleMethods.SetStep(userId, BattleMethods.StepPostBattle);
            }

            return battleLog;
        }
    }
}
