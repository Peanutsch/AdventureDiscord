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
        /// <summary>
        /// Processes a successful hit by applying damage, updating hitpoints,
        /// logging the results, and returning detailed roll information.
        /// </summary>
        /// <param name="userId">The Discord user ID associated with the battle.</param>
        /// <param name="state">The current battle state model.</param>
        /// <param name="weapon">The weapon used in the attack.</param>
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
        public static (int damage, int totalDamage, List<int> rolls, int critRoll, string dice, int newHP) ProcessSuccessfulHit(ulong userId, BattleStateModel state, WeaponModel weapon, int currentHP, bool isPlayerAttacker)
        {
            // Calculate and apply damage, including critical hit or miss logic
            var (damage, totalDamage, rolls, critRoll, dice, newHP) = ProcessRollsAndDamage.RollAndApplyDamage(
                state, weapon, currentHP, isPlayerAttacker);

            if (isPlayerAttacker)
            {
                LogService.Info($"\n\nPlayer's turn > Dice: {dice} Damage: {damage} Crit: {critRoll} Total Damage {totalDamage}\n\n");

                // Player attacks, update NPC HP
                state.CurrentHitpointsNPC = newHP;

                // Track damage in multiplayer encounter system (thread-safe)
                if (!string.IsNullOrEmpty(state.EncounterTileId))
                {
                    var (actualNewHp, isDefeated) = Adventure.Services.ActiveEncounterTracker.RecordDamage(userId, state.EncounterTileId, totalDamage);
                    // Sync HP from thread-safe tracker (prevents race conditions)
                    state.CurrentHitpointsNPC = actualNewHp;
                }

                // Update player's HP in JSON file (even if unchanged) for consistency
                JsonDataManager.UpdatePlayerHitpoints(userId, state.Player.Name!, state.Player.Hitpoints);

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
                JsonDataManager.UpdatePlayerHitpoints(userId, state.Player.Name!, state.Player.Hitpoints);

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
            // Adjust XP based on damage ratio (percentage of NPC HP the player dealt)
            int adjustedRewardXP = (rewardedXP * state.RatioDamageDealt) / 100;

            LogService.Info($"[ProcessSuccesAttack.ProcessXPReward] Base XP: {rewardedXP}, Damage Ratio: {state.RatioDamageDealt}%, Adjusted XP: {adjustedRewardXP}");

            var currentXP = state.Player.XP;
            var newXP = currentXP + adjustedRewardXP;

            // Track reward XP for UI display
            state.RewardXP = adjustedRewardXP;

            // Update XP in memory and JSON
            state.NewTotalXP = newXP;
            state.Player.XP = newXP;
            JsonDataManager.UpdatePlayerXP(state.Player.Id, state.Player.Name!, newXP);

            // Determine new level based on updated XP
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

            // If player has leveled up, update level in memory and JSON
            if (newLevel > oldLevel)
            {
                state.Player.Level = newLevel;
                JsonDataManager.UpdatePlayerLevel(state.Player.Id, state.Player.Name!, newLevel);

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
                JsonDataManager.UpdatePlayerLevel(state.Player.Id, state.Player.Name!, newLevel);

                LogService.Info($"[UpdateLevelFromXP] Player {state.Player.Name} leveled up from {oldLevel} → {newLevel}!");
            }
        }
        #endregion PROCESS XP AND LEVEL
    }
}
