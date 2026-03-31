using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
using Discord;

public static class NpcAttack
{
    public static string ProcessNpcAttack(ulong userId, WeaponModel weapon)
    {
        // Get attack data from AttackProcessor
        (string battleLog, BattleStateModel state) = AttackProcessor.ProcessAttack(userId, weapon, false);

        // If player is dead → end battle
        if (state.Player.Hitpoints <= 0)
        {
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.EndBattle);
            state.EmbedColor = Color.DarkRed;

            battleLog += $"\n\n💀 **{state.Player.Name} has been defeated by {state.Npc.Name}!**";
        }
        else
        {
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.PostBattle);
        }

        return battleLog;
    }
}
