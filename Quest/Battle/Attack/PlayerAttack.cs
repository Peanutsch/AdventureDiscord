using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Discord;

class PlayerAttack
{
    /// <summary>
    /// Processes the player's attack on the NPC, updates the battle state, and generates the battle log. 
    /// Handles the end of the battle and rewards XP to the player if the NPC is defeated.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="weapon"></param>
    /// <returns></returns>
    public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
    {
        // // Get attack data from AttackProcessor
        (string battleLog, BattleStateModel state) = AttackProcessor.ProcessAttack(userId, weapon, true);

        // If NPC is dead → end battle + reward XP
        if (state.CurrentHitpointsNPC <= 0)
        {
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.EndBattle);
            state.EmbedColor = Color.Purple;

            int rewardXP = ChallengeRatingHelpers.GetRewardXP(state.Npc.CR);
            (bool leveledUp, int oldLevel, int newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);
            state.PlayerLeveledUp = leveledUp;  // Track level-up for ASI trigger

            // Generate battle log for victory and XP reward
            battleLog += $"\n\n💀 **VICTORY!!! {state.Npc.Name} is defeated after {state.RoundCounter} {UseOfS(state.RoundCounter)}!**";
            battleLog += $"\n🏆 **{state.Player.Name}** gains **{state.RewardXP} XP** (Total: {state.NewTotalXP} XP)";

            if (leveledUp)
                battleLog += $"\n\n✨ **LEVEL UP!** {state.Player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
        }
        else
        {
            // If NPC is still alive → set next step to PostBattle for NPC's turn
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.PostBattle);
        }

        return battleLog;
    }

    // Helper method to determine correct pluralization of "round(s)"
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
