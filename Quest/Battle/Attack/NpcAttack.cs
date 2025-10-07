using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
using Discord;

public static class NpcAttack
{
    public static string ProcessNpcAttack(ulong userId, WeaponModel weapon)
    {
        // Get attack data from AttackProcessor
        var (battleLog, state) = AttackProcessor.ProcessAttack(userId, weapon, false);

        // If player is dead → end battle
        if (state.Player.Hitpoints <= 0)
        {
            EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepEndBattle);
            state.EmbedColor = Color.DarkRed;

            battleLog += $"\n\n💀 **{state.Player.Name} has been defeated by {state.Npc.Name}!**";
        }
        else
        {
            EncounterBattleStepsSetup.SetStep(userId, EncounterBattleStepsSetup.StepPostBattle);
        }

        return battleLog;
    }
}
