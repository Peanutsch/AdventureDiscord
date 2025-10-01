using Adventure.Data;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Battle.TextGenerator;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;

namespace Adventure.Quest.Battle.Attack
{
    public static class NpcAttack
    {
        public static string ProcessNpcAttack(ulong userId, WeaponModel weapon)
        {
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, false);

            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, false);

            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit ||
                hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, player.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, false);
            }

            TrackHP.GetAndSetHPStatus(state.HitpointsAtStartPlayer, player.Hitpoints, TrackHP.TargetType.Player, state);
            string statusLabel = state.StateOfPlayer;

            string attackResult = AttackResultHelper.GetAttackResult(hitResult);

            string battleLog = BattleTextGenerator.GenerateBattleLog(
                attackResult,
                npc.Name!,
                player.Name!,
                weapon.Name!,
                state.TotalDamage,
                statusLabel,
                GameData.BattleText!,
                state,
                GameData.RollText,
                strength,
                false
            );

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
