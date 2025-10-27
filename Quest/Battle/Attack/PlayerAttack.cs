using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Discord;

class PlayerAttack
{
    public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
    {
        // // Get attack data from AttackProcessor
        var (battleLog, state) = AttackProcessor.ProcessAttack(userId, weapon, true);

        // If NPC is dead → end battle + reward XP
        if (state.CurrentHitpointsNPC <= 0)
        {
            EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepEndBattle);
            state.EmbedColor = Color.Purple;

            var rewardXP = ChallengeRatingHelpers.GetRewardXP(state.Npc.CR);
            var (leveledUp, oldLevel, newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);

            battleLog += $"\n\n💀 **VICTORY!!! {state.Npc.Name} is defeated after {state.RoundCounter} {UseOfS(state.RoundCounter)}!**";
            battleLog += $"\n🏆 **{state.Player.Name}** gains **{state.RewardXP} XP** (Total: {state.NewTotalXP} XP)";

            if (leveledUp)
                battleLog += $"\n\n✨ **LEVEL UP!** {state.Player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
        }   
        else
        {
            EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepPostBattle);
        }

        return battleLog;
    }

    public static string UseOfS(int round)
    {
        if (round > 0)
        {
            return "rounds";
        }
        else
        {
            return "round";
        }
    }
}
