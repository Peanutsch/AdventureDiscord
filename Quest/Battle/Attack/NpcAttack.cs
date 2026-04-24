using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
using Discord;

public static class NpcAttack
{
    /// <summary>
    /// Processes an NPC attack against the player. It calculates the attack outcome using the AttackProcessor, updates the battle state, 
    /// and returns a battle log message describing the attack and its consequences. 
    /// If the player's hitpoints drop to 0 or below, it sets the battle step to EndBattle and updates the embed color to indicate defeat. 
    /// Otherwise, it proceeds to the PostBattle step.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="weapon"></param>
    /// <returns></returns>
    public static string ProcessNpcAttack(ulong userId, WeaponModel weapon)
    {
        // Get attack data from AttackProcessor
        (string battleLog, BattleStateModel state) = AttackProcessor.ProcessAttack(userId, weapon, false);

        // If player is dead → end battle
        if (state.Player.Hitpoints <= 0)
        {
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.EndBattle);
            state.EmbedColor = Color.DarkRed;

            // Remove player from encounter (not entire encounter - other players may still be fighting)
            Adventure.Services.ActiveEncounterTracker.RemovePlayerFromEncounter(userId);

            // Load correct player name for defeat message (important for multiplayer)
            var defeatedPlayer = Adventure.Data.PlayerDataManager.LoadByUserId(userId);
            battleLog += $"\n\n💀 **{defeatedPlayer.Name} has been defeated by {state.Npc.Name}!**";
        }
        else
        {
            EncounterBattleStepsSetup.SetStep(userId, BattleStep.PostBattle);
        }

        return battleLog;
    }
}
