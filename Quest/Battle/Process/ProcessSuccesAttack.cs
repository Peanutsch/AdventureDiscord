using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Rolls;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle.Process
{
    class ProcessSuccesAttack
    {
        #region PROCESS SUCCESFULL ATTACK
        // Add English comments and summaries
        /// <summary>
        /// Processes a successful hit by applying damage, updating hitpoints,
        /// logging the results, and returning detailed roll information.
        /// </summary>
        /// <param name="userId">The Discord user ID associated with the battle.</param>
        /// <param name="state">The current battle state model.</param>
        /// <param name="weapon">The weapon used in the attack.</param>
        /// <param name="strength">The attacker's strength modifier.</param>
        /// <param name="currentHP">The defender's current hitpoints before the hit.</param>
        /// <param name="isPlayerAttacker">Whether the player is the attacker (true) or the creature (false).</param>
        /// <returns>
        /// A tuple containing:
        /// - damage: Raw damage from base dice
        /// - totalDamage: Final damage after modifiers (and critical hit if applicable)
        /// - rolls: List of individual dice rolls
        /// - critRoll: Additional dice roll used for critical hit (0 if not a crit)
        /// - dice: Dice notation string (e.g. "2d6")
        /// - newHP: The defender's new HP after damage is applied
        /// </returns>
        public static (int damage, int totalDamage, List<int> rolls, int critRoll, string dice, int newHP) ProcessSuccessfulHit(ulong userId, BattleStateModel state, WeaponModel weapon, int strength, int currentHP, bool isPlayerAttacker)
        {
            // Calculate and apply damage, including critical hit or miss logic
            var (damage, totalDamage, rolls, critRoll, dice, newHP) = ProcessRollsAndDamage.RollAndApplyDamage(
                state, weapon, strength, currentHP, isPlayerAttacker);

            if (isPlayerAttacker)
            {
                LogService.Info($"Player's turn > Dice: {dice} {rolls} Damage: {damage} Crit: {critRoll} Total Damage {totalDamage}");

                // Player attacks, update NPC HP
                state.CurrentHitpointsNPC = newHP;

                // Update player's HP in JSON file (even if unchanged) for consistency
                JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

                // Log HP status after player's attack
                LogService.Info($"[ProcessSuccesAttack.ProcessSuccessfulHit] After player attack\n" +
                                $"HP {state.Player.Name}: {state.Player.Hitpoints}\n" +
                                $"HP Creature: {state.CurrentHitpointsNPC}");
            }
            else
            {
                LogService.Info($"NPC's turn > Dice: {dice} Damage: {damage} Crit: {critRoll} Total Damage {totalDamage}");

                // NPC attacks, update player HP
                state.Player.Hitpoints = newHP;

                // Update player's HP in JSON
                JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

                // Log HP status after creature's attack
                LogService.Info($"[ProcessSuccesAttack.ProcessSuccessfulHit] After NPC attack\n" +
                                $"HP Player: {state.Player.Hitpoints} VS HP NPC: {state.CurrentHitpointsNPC}");
            }

            // Return detailed result of the damage roll
            return (damage, totalDamage, rolls, critRoll, dice, newHP);
        }
        #endregion PROCESS SUCCESFULL ATTACK

        #region PROCESS XP
        public static void ProcessSaveXPReward(int rewardedXP, BattleStateModel state)
        {
            var currentXP = state.Player.XP;
            var newXP =  currentXP + rewardedXP;

            LogService.Info($"[ProcessSuccesAttack.ProcessSaveXPReward] Player: {state.Player.Name} XP Reward: {rewardedXP} Current XP: {state.Player.XP} New XP: {newXP}");

            // Update player's XP in JSON and BattleState
            state.NewTotalXP = newXP;
            JsonDataManager.UpdatePlayerXPInJson(state.Player.Id, state.Player.Name!, newXP);
        }
        #endregion PROCESS XP
    }
}
