using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Battle.TextGenerator;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Adventure.Services;
using Discord;
using System.Numerics;

namespace Adventure.Quest.Battle.Attack
{
    /// <summary>
    /// Handles player attacks during battle, including hit validation, damage calculation, 
    /// HP updates, battle log generation, and XP rewards.
    /// </summary>
    class PlayerAttack
    {
        /// <summary>
        /// Processes a player's attack on an NPC.
        /// </summary>
        /// <param name="userId">The Discord user ID of the player.</param>
        /// <param name="weapon">The weapon being used for the attack.</param>
        /// <returns>A formatted string containing the battle log and status updates.</returns>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            // Step 1: Determine the result of the attack (hit, miss, critical hit, critical miss)
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: true);

            // Step 2: Retrieve battle participants and current state
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: true);

            // Step 3: If the attack hits, calculate the total damage (including criticals)
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit ||
                hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, state.CurrentHitpointsNPC) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, state.CurrentHitpointsNPC, isPlayerAttacker: true);
            }

            // Step 4: Update NPC's HP status after the attack
            TrackHP.GetAndSetHPStatus(
                state.HitpointsAtStartNPC,
                state.CurrentHitpointsNPC,
                TrackHP.TargetType.NPC,
                state
            );
            string statusLabel = state.StateOfNPC;

            // Step 5: Map hit result to a text category (criticalHit, hit, miss, criticalMiss)
            string attackResult = AttackResultHelper.GetAttackResult(hitResult);

            // Step 6: Generate a battle log using the narrative text templates (JSON)
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
                isPlayerAttack: true // Show dice rolls for player attacks
            );

            // Step 7: Handle defeated NPCs, reward XP, and check for level up
            if (state.CurrentHitpointsNPC <= 0)
            {
                // Mark the battle as ended
                EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepEndBattle);

                // Calculate XP reward and check for level up
                var rewardXP = ChallengeRatingHelpers.GetRewardXP(state.Npc.CR);
                var (leveledUp, oldLevel, newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);

                // Add defeat and XP info to the battle log
                battleLog += $"\n\n💀 **{npc.Name} is defeated!**";
                battleLog += $"\n🏆 **{player.Name}** gains **{state.RewardXP} XP** (Total: {state.NewTotalXP} XP)";

                if (leveledUp)
                    battleLog += $"\n\n✨ **LEVEL UP!** {player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
            }
            else
            {
                // Mark battle step as post-attack continuation
                EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepPostBattle);
            }

            // Step 8: Return the fully formatted battle log
            return battleLog;
        }
    }
}
