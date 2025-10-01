using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Helpers;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Rolls
{
    public class ProcessRollsAndDamage
    {
        #region VALIDATE HIT
        /// <summary>
        /// Represents the result of an attack attempt.
        /// </summary>
        public enum HitResult
        {
            IsCriticalHit,
            IsCriticalMiss,
            IsValidHit,
            IsMiss
        }

        /// <summary>
        /// Performs an attack roll to determine whether the hit is successful, critical, or missed.
        /// </summary>
        /// <param name="userId">The Discord user ID of the attacker.</param>
        /// <param name="isPlayerAttacker">True if the player is attacking, false if the creature is attacking.</param>
        /// <returns>The result of the hit attempt (hit, miss, critical, etc.).</returns>
        public static HitResult ValidateHit(ulong userId, bool isPlayerAttacker)
        {
            var state = BattleStateSetup.GetBattleState(userId);

            // Perform the attack roll (1d20)
            int attackRoll = DiceRoller.RollWithoutDetails(1, 20);

            int abilityStrength;
            int levelCR;
            int defenderAC;

            string turn;

            // Determine attacking and defending stats based on who is attacking
            if (isPlayerAttacker)
            {
                // Ensure the creature has valid armor
                state.Npc.ArmorElements = state.NpcArmor.FirstOrDefault() ?? new ArmorModel();

                turn = "Player's";
                levelCR = state.Player.Level;
                abilityStrength = state.Player.Attributes.Strength;
                defenderAC = state.Npc.ArmorElements.ArmorClass;
            }
            else
            {
                // Ensure the player has valid armor
                state.Player.ArmorElements = state.PlayerArmor.FirstOrDefault() ?? new ArmorModel();

                turn = "NPC's";
                abilityStrength = state.Npc.Attributes.Strength;
                levelCR = (int)state.Npc.CR;
                defenderAC = state.Player.ArmorElements.ArmorClass;
            }

            // Get modifiers
            int proficiencyModifier = ModifierHelpers.GetProficiencyModifier(levelCR);
            int abilityModifier = ModifierHelpers.GetAbilityModifier(abilityStrength);

            // Calculate total attack value
            int totalAttackRoll = attackRoll + abilityModifier + proficiencyModifier;

            LogService.Info($"[ProcessRollsAndDamage.ValidateHit] Calculating totalAttackRoll:\n\n" +
                            $"> totalAttackRoll({totalAttackRoll}) = attackRoll(+{attackRoll}) + abilityModifier[{abilityStrength}](+{abilityModifier}) + proficiencyModifier(+{proficiencyModifier})\n\n");

            // Store relevant data in the battle state
            state.AttackRoll = attackRoll;
            state.ProficiencyModifier = proficiencyModifier;
            state.AbilityModifier = abilityModifier;
            state.TotalAttackRoll = totalAttackRoll;
            state.ArmorElements.ArmorClass = defenderAC;
            state.IsCriticalHit = attackRoll == 20;   // Natural 20 = critical hit
            state.IsCriticalMiss = attackRoll == 1;   // Natural 1 = critical miss

            // Log the calculation details for debugging
            LogService.Info($"[ProcessRollsAndDamage.ValidateHit]\n{turn}'s turn:\n" +
                            $"abilityStrength: {abilityStrength}\n" +
                            $"attackRoll: +{attackRoll}\n" +
                            $"proficiencyModifier: +{proficiencyModifier}\n\n" +
                            $"> totalRoll: {totalAttackRoll}\n" +
                            $"> defenderAC: {defenderAC}\n\n");

            // Determine and return the hit result
            if (state.IsCriticalHit) {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Critical Hit");
                state.HitResult = "isCriticalHit";

                return HitResult.IsCriticalHit;
            }
            else if (state.IsCriticalMiss) {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Critical Miss");
                state.HitResult = "isCriticalMiss";

                return HitResult.IsCriticalMiss;
            }
            else if (totalAttackRoll >= defenderAC) {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Hit");
                state.HitResult = "isHit";

                return HitResult.IsValidHit;
            }
            else {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Miss");
                state.HitResult = "isMiss";

                return HitResult.IsMiss;
            }
        }
        #endregion VALIDATE HIT

        #region PROCESS ROLL AND DAMAGE
        /// <summary>
        /// Rolls weapon damage and applies it to the target. 
        /// Handles critical hits (double damage) and critical misses (no damage).
        /// </summary>
        /// <param name="state">The current battle state of the player and creature.</param>
        /// <param name="weapon">The weapon being used to attack.</param>
        /// <param name="attackerStrength">The strength modifier of the attacker.</param>
        /// <param name="currentHitpoints">The current HP of the defender before damage is applied.</param>
        /// <param name="isPlayerAttacker">True if the player is attacking, false if the creature is attacking.</param>
        /// <returns>
        /// A tuple containing:
        /// - Raw damage roll
        /// - Total damage after strength modifier (and critical adjustments)
        /// - List of individual damage dice rolls
        /// - Additional critical roll (only used if critical hit)
        /// - Dice notation string
        /// - New HP of the defender after damage
        /// </returns>
        public static (int damage, int totalDamage, List<int> rolls, int critRoll, string diceNotation, int newHP) RollAndApplyDamage(BattleStateModel state, WeaponModel weapon, int attackerStrength, int currentHitpoints, bool isPlayerAttacker)
        {
            // Get weapon damage dice config
            var diceCount = weapon.Damage.DiceCount;
            var diceValue = weapon.Damage.DiceValue;

            // Roll normal damage and store individual dice results
            var (damage, rolls) = DiceRoller.RollWithDetails(diceCount, diceValue);

            // Roll additional damage for critical hit (same dice as base damage)
            var critRoll = DiceRoller.RollWithoutDetails(diceCount, diceValue);

            // Format dice notation, e.g., "1d8"
            var dice = $"{diceCount}d{diceValue}";

            // Base damage: normal roll + strength/ability modifier
            var totalDamage = damage + state.AbilityModifier;
            LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] Calculating totalDamage:\n\n" +
                            $"totalDamage({totalDamage}) = damage({damage}) + abilityModifier({state.AbilityModifier})\n\n");

            // Critical extra damage
            var totalCritDamage = damage + critRoll + state.AbilityModifier;
            LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] Calculating totalCriticalDamage:\n\n" +
                            $"totalCritDamage({totalCritDamage}) = damage({damage}) + critRoll({critRoll}) + abilityModifier({state.AbilityModifier})\n\n");

            // Apply critical-hit rule first
            if (state.IsCriticalHit)
            {
                totalDamage = totalCritDamage;
            }

            // Apply critical-miss rule (no damage)
            if (state.IsCriticalMiss)
            {
                totalDamage = 0;
            }

            // Calculate new HP, ensuring it doesn't go below 0
            var newHP = currentHitpoints - totalDamage;
            if (newHP < 0)
                newHP = 0;

            // Store all derived values in state (after all adjustments)
            state.Damage = damage;
            state.CritRoll = critRoll;
            state.Rolls = rolls;
            state.Dice = dice;
            state.TotalDamage = totalDamage;
            state.LastUsedWeapon = weapon.Name ?? state.LastUsedWeapon;

            // Store pre-damage HP for logging/visualization
            if (isPlayerAttacker)
            {
                var preSavedHPNpc = state.PreHpNPC;
                state.PreHpNPC = currentHitpoints - totalDamage;
                LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage]\n\npre HP NPC: {preSavedHPNpc}\nUpdated state.PreHPNPC to: {state.PreHpNPC}\n\n");
            }
            else
            {
                var preSavedHPPlayer = state.PreHpPlayer;
                state.PreHpPlayer = currentHitpoints - totalDamage;
                LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage]\n\npre HP Player: {preSavedHPPlayer}\nUpdated state.PreHPPlayer to: {state.PreHpPlayer}\n\n");
            }

            // Return tuple with detailed damage info (damage = raw dice sum, totalDamage = final after mods/crit/miss)
            return (damage, totalDamage, rolls, critRoll, dice, newHP);
        }
        #endregion PROCESS ROLL AND DAMAGE
    }
}
