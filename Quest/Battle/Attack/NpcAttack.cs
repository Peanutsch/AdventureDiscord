using Adventure.Data;
using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.TextGenerator;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Adventure.Services;

namespace Adventure.Quest.Battle.Attack
{
    class NpcAttack
    {
        /// <summary>
        /// Handles an NPC attack on the player, using JSON-based narrative text + HP status.
        /// </summary>
        public static string ProcessNpcAttack(ulong userId, WeaponModel weapon)
        {
            // Determine hit result
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: false);

            // Get participants and state
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: false);

            // If hit or critical hit, process damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit ||
                hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, player.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, isPlayerAttacker: false);
            }

            // Update Player HP status
            TrackHP.GetAndSetHPStatus(state.HitpointsAtStartPlayer, player.Hitpoints, TrackHP.TargetType.Player, state);
            string statusLabel = state.StateOfPlayer;

            // Map hit result to helper key (criticalHit, hit, miss, criticalMiss)
            string attackResult = AttackResultHelper.GetAttackResult(hitResult);

            // Build narrative log (NPC side = no dice rolls shown)
            string battleLog = BattleTextGenerator.GenerateBattleLog(
                attackResult: attackResult,
                attacker: npc.Name!,
                defender: player.Name!,
                weapon: weapon.Name!,
                damage: state.TotalDamage,
                statusLabel: statusLabel,
                battleText: GameData.BattleText!,
                state: state,
                rollText: GameData.RollText,
                strength: strength,
                isPlayerAttack: false
            );

            // Add HP status line if the player is still alive
            if (statusLabel != "Dead" && GameData.BattleText!.HpStatus.TryGetValue(statusLabel, out var hpText))
            {
                battleLog += $"\n\n❤️ **{player.Name} {hpText}**";
            }

            // Check if the Player is defeated
            if (player.Hitpoints <= 0)
            {
                EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepEndBattle);
                battleLog += $"\n\n💀 **{player.Name} has been defeated by {npc.Name}!**";
            }
            else
            {
                EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepPostBattle);
            }

            return battleLog;
        }
    }
}
