using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
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
            var session = BattleStateSetup.GetBattleSession(userId);

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
                session.Context.Npc.ArmorElements = session.Context.NpcArmor.FirstOrDefault() ?? new ArmorModel();

                turn = "Player's";
                levelCR = session.Context.Player.Level;
                abilityStrength = session.Context.Player.Attributes.Strength;
                defenderAC = session.Context.Npc.ArmorElements.ArmorClass;
            }
            else
            {
                // Ensure the player has valid armor
                session.Context.Player.ArmorElements = session.Context.PlayerArmor.FirstOrDefault() ?? new ArmorModel();

                turn = "NPC's";
                abilityStrength = session.Context.Npc.Attributes.Strength;
                levelCR = (int)session.Context.Npc.CR;
                defenderAC = session.Context.Player.ArmorElements.ArmorClass;
            }

            // Get modifiers
            int proficiencyModifier = ModifierHelpers.GetProficiencyModifier(levelCR);
            int abilityModifier = ModifierHelpers.GetAbilityModifier(abilityStrength);

            // Calculate total attack value
            int totalAttackRoll = attackRoll + abilityModifier + proficiencyModifier;

            LogService.Info($"[ProcessRollsAndDamage.ValidateHit] Calculating totalAttackRoll: totalAttackRoll({totalAttackRoll}) = attackRoll(+{attackRoll}) + abilityModifier[{abilityStrength}](+{abilityModifier}) + proficiencyModifier(+{proficiencyModifier})\n");

            // Store relevant data in the battle state
            session.State.AttackRoll = attackRoll;
            session.State.ProficiencyModifier = proficiencyModifier;
            session.State.AbilityModifier = abilityModifier;
            session.State.TotalAttackRoll = totalAttackRoll;
            session.Context.ArmorElements.ArmorClass = defenderAC;
            session.State.IsCriticalHit = attackRoll == 20;   // Natural 20 = critical hit
            session.State.IsCriticalMiss = attackRoll == 1;   // Natural 1 = critical miss

            // Log the calculation details for debugging
            LogService.Info($"[ProcessRollsAndDamage.ValidateHit] {turn}'s turn:\n" +
                            $"abilityStrength: {abilityStrength}\n" +
                            $"attackRoll: +{attackRoll}\n" +
                            $"proficiencyModifier: +{proficiencyModifier}\n" +
                            $"> totalRoll: {totalAttackRoll}\n" +
                            $"> defenderAC: {defenderAC}\n");

            // Determine and return the hit result
            if (session.State.IsCriticalHit) {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Critical Hit");
                session.State.HitResult = "isCriticalHit";

                return HitResult.IsCriticalHit;
            }
            else if (session.State.IsCriticalMiss) {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Critical Miss");
                session.State.HitResult = "isCriticalMiss";

                return HitResult.IsCriticalMiss;
            }
            else if (totalAttackRoll >= defenderAC) {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Hit");
                session.State.HitResult = "isHit";

                return HitResult.IsValidHit;
            }
            else {
                LogService.Info($"[ProcessRollsAndDamage.ValidateHit] HitResult: Miss");
                session.State.HitResult = "isMiss";

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
        public static (int damage, int totalDamage, List<int> rolls, int critRoll, string diceNotation, int newHP) RollAndApplyDamage(BattleSession session, WeaponModel weapon, int currentHitpoints, bool isPlayerAttacker)
        {
            // Get weapon damage dice config
            var diceCount = weapon.Damage.DiceCount;
            var diceValue = weapon.Damage.DiceValue;

            // Roll normal damage and store individual dice results
            LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] Rolling for Damage...");
            var (damage, rolls) = DiceRoller.RollWithDetails(diceCount, diceValue);

            // Roll additional damage for critical hit (same dice as base damage)
            LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] Rolling for Critical Damage...");
            var critRoll = DiceRoller.RollWithoutDetails(diceCount, diceValue);

            // Format dice notation, e.g., "1d8"
            var dice = $"{diceCount}d{diceValue}";

            // Base damage: normal roll + strength/ability modifier
            var totalDamage = damage + session.State.AbilityModifier;
            LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] Calculating totalDamage: totalDamage({totalDamage}) = damage({damage}) + abilityModifier({session.State.AbilityModifier})\n");

            // Critical extra damage
            var totalCritDamage = damage + critRoll + session.State.AbilityModifier;
            LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] Calculating totalCriticalDamage: totalCritDamage({totalCritDamage}) = damage({damage}) + critRoll({critRoll}) + abilityModifier({session.State.AbilityModifier})\n");

            // Apply critical-hit rule first
            if (session.State.IsCriticalHit)
            {
                totalDamage = totalCritDamage;
            }

            // Apply critical-miss rule (no damage)
            // Ensure TotalDamage is not less then 0, combined with when totalDamage and totalCritDamage < = 0
            if (session.State.IsCriticalMiss || totalDamage <= 0 || totalCritDamage <= 0) {
                totalDamage = 0;
            }

            // Calculate new HP, ensuring it doesn't go below 0
            var newHP = currentHitpoints - totalDamage;
            if (newHP < 0)
                newHP = 0;

            // Store all derived values in state (after all adjustments)
            session.State.Damage = damage;
            session.State.CritRoll = critRoll;
            session.State.Rolls = rolls;
            session.State.Dice = dice;
            session.State.TotalDamage = totalDamage;
            session.State.LastUsedWeapon = weapon.Name ?? session.State.LastUsedWeapon;

            // Store pre-damage HP for logging/visualization
            if (isPlayerAttacker)
            {
                var preSavedHPNpc = session.State.PreHpNPC;
                session.State.PreHpNPC = currentHitpoints - totalDamage;
                LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] pre HP NPC: {preSavedHPNpc} > Updated state.PreHPNPC to: {session.State.PreHpNPC}\n");
            }
            else
            {
                var preSavedHPPlayer = session.State.PreHpPlayer;
                session.State.PreHpPlayer = currentHitpoints - totalDamage;
                LogService.Info($"[ProcessRollAndApplyDamage.RollAndApplyDamage] pre HP Player: {preSavedHPPlayer} > Updated state.PreHPPlayer to: {session.State.PreHpPlayer}\n");
            }

            // Return tuple with detailed damage info (damage = raw dice sum, totalDamage = final after mods/crit/miss)
            return (damage, totalDamage, rolls, critRoll, dice, newHP);
        }
        #endregion PROCESS ROLL AND DAMAGE
    }
}
