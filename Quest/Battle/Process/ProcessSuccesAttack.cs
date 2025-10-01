using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Helpers;
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
                LogService.Info($"\n\nPlayer's turn > Dice: {dice} Damage: {damage} Crit: {critRoll} Total Damage {totalDamage}\n\n");

                // Player attacks, update NPC HP
                state.CurrentHitpointsNPC = newHP;

                // Update player's HP in JSON file (even if unchanged) for consistency
                JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

                // Log HP status after player's attack
                LogService.Info($"[ProcessSuccesAttack.ProcessSuccessfulHit] After player attack:\n\n" +
                                $"HP {state.Player.Name}: {state.Player.Hitpoints} HP NPC: {state.CurrentHitpointsNPC}\n\n");
            }
            else
            {
                LogService.Info($"\n\nNPC's turn > Dice: {dice} Damage: {damage} Crit: {critRoll} Total Damage {totalDamage}\n\n");

                // NPC attacks, update player HP
                state.Player.Hitpoints = newHP;

                // Update player's HP in JSON
                JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

                // Log HP status after creature's attack
                LogService.Info($"[ProcessSuccesAttack.ProcessSuccessfulHit] After NPC attack:\n\n" +
                                $"HP Player: {state.Player.Hitpoints} HP NPC: {state.CurrentHitpointsNPC}\n\n");
            }

            // Return detailed result of the damage roll
            return (damage, totalDamage, rolls, critRoll, dice, newHP);
        }
        #endregion PROCESS SUCCESFULL ATTACK

        #region PROCESS XP AND LEVEL
        public static (bool leveledUp, int oldLevel, int newLevel) ProcessXPReward(int rewardedXP, BattleStateModel state)
        {
            var currentXP = state.Player.XP;
            var newXP = currentXP + rewardedXP;

            // Update XP in memory en JSON
            state.NewTotalXP = newXP;
            state.Player.XP = newXP;
            JsonDataManager.UpdatePlayerXPInJson(state.Player.Id, state.Player.Name!, newXP);

            // Bepaal huidig en nieuw level
            int oldLevel = state.Player.Level;
            int newLevel = 1;

            for (int level = LevelHelpers.LevelXPThresholds.Length; level > 0; level--)
            {
                if (newXP >= LevelHelpers.LevelXPThresholds[level - 1])
                {
                    newLevel = level;
                    break;
                }
            }

            // Check of speler een level up heeft
            if (newLevel > oldLevel)
            {
                state.Player.Level = newLevel;
                JsonDataManager.UpdatePlayerLevelInJson(state.Player.Id, state.Player.Name!, newLevel);

                return (true, oldLevel, newLevel);
            }

            return (false, oldLevel, oldLevel);
        }


        /// <summary>
        /// Updates the player's level in state and JSON based on their XP.
        /// </summary>
        public static void UpdateLevelFromXP(BattleStateModel state)
        {
            int xp = state.Player.XP;
            int newLevel = 1;

            for (int level = LevelHelpers.LevelXPThresholds.Length; level > 0; level--)
            {
                if (xp >= LevelHelpers.LevelXPThresholds[level - 1])
                {
                    newLevel = level;
                    break;
                }
            }

            if (newLevel > state.Player.Level)
            {
                //int oldLevel = state.Player.Level;
                int oldLevel = state.Player.Level;
                state.Player.Level = newLevel;

                // Update JSON
                JsonDataManager.UpdatePlayerLevelInJson(state.Player.Id, state.Player.Name!, newLevel);

                LogService.Info($"[UpdateLevelFromXP] Player {state.Player.Name} leveled up from {oldLevel} → {newLevel}!");
            }
        }
        #endregion PROCESS XP AND LEVEL
    }
}
