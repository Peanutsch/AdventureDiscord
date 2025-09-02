using Adventure.Models.Items;
using Adventure.Services;
using Adventure.Quest.Dice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Numerics;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Creatures;
using Adventure.Models.Player;

namespace Adventure.Quest.Battle
{
    class BattleEngineHelpers
    {
        private static readonly Random rng = new();

        #region GET DATA

        /// <summary>
        /// Retrieves the current battle state, player, creature, and attacker's strength.
        /// </summary>
        /// <param name="userId">The Discord user ID involved in the battle.</param>
        /// <param name="playerIsAttacker">True if the player is the attacker, false if the creature is.</param>
        /// <returns>A tuple containing the battle state, player model, creature model, and attacker strength.</returns>
        public static (BattleStateModel state, PlayerModel player, CreaturesModel creature, int attackerStrength) GetBattleParticipants(ulong userId, bool playerIsAttacker)
        {
            var state = BattleEngine.GetBattleState(userId);
            var player = state.Player;
            var creature = state.Creatures;

            // Determine the strength value of the attacker (player or creature)
            int attackerStrength = playerIsAttacker ? player.Attributes.Strength : creature.Attributes.Strength;

            return (state, player, creature, attackerStrength);
        }

        #endregion GET DATA

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
            var state = BattleEngine.GetBattleState(userId);

            // Perform the attack roll (1d20)
            int attackRoll = DiceRoller.RollWithoutDetails(1, 20);

            int attackStrength;
            int defenderAC;

            // Determine attacking and defending stats based on who is attacking
            if (isPlayerAttacker)
            {
                // Ensure the creature has valid armor
                state.Creatures.ArmorElements = state.CreatureArmor.FirstOrDefault() ?? new ArmorModel();
                attackStrength = state.Player.Attributes.Strength;
                defenderAC = state.Creatures.ArmorElements.ArmorClass;
            }
            else
            {
                // Ensure the player has valid armor
                state.Player.ArmorElements = state.PlayerArmor.FirstOrDefault() ?? new ArmorModel();
                attackStrength = state.Creatures.Attributes.Strength;
                defenderAC = state.Player.ArmorElements.ArmorClass;
            }

            // Get ability modifier
            int abilityMod = ProcessRollsAndDamage.GetModifier(attackStrength);

            // Calculate total attack value
            int totalRoll = attackRoll + abilityMod;

            // Store relevant data in the battle state
            state.AttackRoll = attackRoll;
            state.AbilityMod = abilityMod;
            state.TotalRoll = totalRoll;
            state.ArmorElements.ArmorClass = defenderAC;
            state.IsCriticalHit = attackRoll == 20;   // Natural 20 = critical hit
            state.IsCriticalMiss = attackRoll == 1;   // Natural 1 = critical miss

            // Log the calculation details for debugging
            LogService.Info($"[BattleEngineHelpers.ValidateHit]\n" +
                            $"attackRoll: {attackRoll}\n" +
                            $"attackModifier: {attackStrength}\n" +
                            $"totalRoll: {totalRoll}\n" +
                            $"defenderAC: {defenderAC}");

            // Determine and return the hit result
            if (state.IsCriticalHit)
                return HitResult.IsCriticalHit;
            else if (state.IsCriticalMiss)
                return HitResult.IsCriticalMiss;
            else if (totalRoll >= defenderAC)
                return HitResult.IsValidHit;
            else
                return HitResult.IsMiss;
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
        public static (int damage, int totalDamage, List<int> rolls, int critRoll, string diceNotation, int newHP) RollAndApplyDamage(
                        BattleStateModel state,
                        WeaponModel weapon,
                        int attackerStrength,
                        int currentHitpoints,
                        bool isPlayerAttacker)
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

            // Get level/challenge rating
            var bonusModifier = ProcessRollsAndDamage.GetModifier(attackerStrength);

            // Base damage: normal roll + strength modifier
            var totalDamage = damage + bonusModifier;

            // Critical hit: add extra dice roll to damage
            var totalCritDamage = damage + critRoll + bonusModifier;

            // Apply critical hit rules
            if (state.IsCriticalHit)
            {
                totalDamage = totalCritDamage;
            }

            // Apply critical miss rules (no damage)
            if (state.IsCriticalMiss)
            {
                totalDamage = 0;
            }

            // Store pre-damage HP for logging/visualization
            if (isPlayerAttacker)
                state.PreCreatureHP = currentHitpoints + totalDamage;
            else
                state.PrePlayerHP = currentHitpoints + totalDamage;

            // Calculate new HP, ensuring it doesn't go below 0
            var newHP = currentHitpoints - totalDamage;
            if (newHP < 0)
                newHP = 0;

            // Store damage and weapon used in the battle state
            state.Damage = totalDamage;
            state.LastUsedWeapon = weapon.Name!;

            // Return tuple with detailed damage info
            return (damage, totalDamage, rolls, critRoll, dice, newHP);
        }

        #endregion PROCESS ROLL AND DAMAGE

        #region PROCESS ATTACK
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
        private static (int damage, int totalDamage, List<int> rolls, int critRoll, string dice, int newHP) ProcessSuccessfulHit(
            ulong userId,
            BattleStateModel state,
            WeaponModel weapon,
            int strength,
            int currentHP,
            bool isPlayerAttacker)
        {
            // Calculate and apply damage, including critical hit or miss logic
            var (damage, totalDamage, rolls, critRoll, dice, newHP) = RollAndApplyDamage(
                state, weapon, strength, currentHP, isPlayerAttacker);

            if (isPlayerAttacker)
            {
                // Player attacks, update creature HP
                state.Creatures.Hitpoints = newHP;

                // Update player's HP in JSON file (even if unchanged) for consistency
                JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

                // Log HP status after player's attack
                LogService.Info($"[BattleEngine.ProcessPlayerAttack] After player attack\n" +
                                $"HP {state.Player.Name}: {state.Player.Hitpoints}\n" +
                                $"HP Creature: {state.Creatures.Hitpoints}");
            }
            else
            {
                // Creature attacks, update player HP
                state.Player.Hitpoints = newHP;

                // Update player's HP in JSON
                JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

                // Log HP status after creature's attack
                LogService.Info($"[BattleEngine.ProcessCreatureAttack] After creature attack\n" +
                                $"HP Player: {state.Player.Hitpoints}\n" +
                                $"HP Creature: {state.Creatures.Hitpoints}");
            }

            // Return detailed result of the damage roll
            return (damage, totalDamage, rolls, critRoll, dice, newHP);
        }


        /// <summary>
        /// Processes the player's attack during battle, calculates hit or miss based on dice roll and strength modifier,
        /// handles critical hits/misses, and returns a descriptive battle message.
        /// </summary>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            // Determine hit result from attack roll (miss, hit, critical, etc.)
            var hitResult = ValidateHit(userId, isPlayerAttacker: true);

            // Get combat state and both combatants
            var (state, player, creature, strength) = GetBattleParticipants(userId, playerIsAttacker: true);

            int critRoll = 0, damage = 0, totalDamage = 0;
            List<int> rolls = new();
            string dice = "";

            // If hit is successful or critical, calculate damage
            if (hitResult == HitResult.IsValidHit || hitResult == HitResult.IsCriticalHit)
            {
                (damage, totalDamage, rolls, critRoll, dice, creature.Hitpoints) =
                    ProcessSuccessfulHit(userId, state, weapon, strength, creature.Hitpoints, isPlayerAttacker: true);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isCriticalHit");

                    if (creature.Hitpoints <= 0)
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Attack{damage} + Critical({critRoll}) + {state.AbilityMod}(STR({strength})) = `{totalDamage}`\n\n" +
                            $"💀 **{creature.Name} is defeated!**\n";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Attack{damage} + Critical({critRoll}) + {state.AbilityMod}(STR({strength})) = `{totalDamage}`\n\n" +
                            $"🧟 **{creature.Name}** has **{creature.Hitpoints} HP** left.\n";
                    }
                    break;
                
                // CRITICAL MISS
                case HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isCriticalMiss");

                    result =
                        $"🗡️ **{player.Name} attacks {creature.Name}, but critically misses!**\n" +
                        $"🎯 **[CRITICAL MISS]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n\n" +
                        $"🧟 **{creature.Name}** remains unscathed with **{creature.Hitpoints} HP**!";
                    break;

                // HIT
                case HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isValidHit");

                    if (creature.Hitpoints <= 0)
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[HIT]** Attack Roll( { state.AttackRoll } ) + {state.AbilityMod}(STR({strength}) ) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Atack({damage}) + {state.AbilityMod}(STR({strength})) = `{totalDamage}`\n\n" +
                            $"💀 **{creature.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityMod}(STR({strength}) ) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Atack({damage}) + {state.AbilityMod}(STR({strength})) = `{totalDamage}`\n\n" +
                            $"🧟 **{creature.Name}** has **{creature.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] IsMiss");

                    result =
                        $"🗡️ **{player.Name} attacks {creature.Name}, but the {weapon.Name} bounces off!**\n" +
                        $"🎯 **[MISS]** Attack Roll( {state.AttackRoll} ) + {state.AbilityMod}(STR({strength}) ) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{creature.Name}** has **{creature.Hitpoints}** HP left.";
                    break;
            }

            return result;
        }

        /// <summary>
        /// Processes the NPC's attack during battle, calculates hit or miss based on dice roll and strength modifier,
        /// handles critical hits/misses, and returns a descriptive battle message.
        /// </summary>
        public static string ProcessCreatureAttack(ulong userId, WeaponModel weapon)
        {
            var hitResult = ValidateHit(userId, isPlayerAttacker: false);
            var (state, player, creature, strength) = GetBattleParticipants(userId, playerIsAttacker: false);

            int critRoll = 0, damage = 0, totalDamage = 0;
            List<int> rolls = new();
            string dice = "";

            if (hitResult == HitResult.IsValidHit || hitResult == HitResult.IsCriticalHit)
            {
                (damage, totalDamage, rolls, critRoll, dice, player.Hitpoints) =
                    ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, isPlayerAttacker: false);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsCriticalHit");

                    if (player.Hitpoints <= 0)
                    {
                        result =
                            $"🗡️ **{creature.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): {critRoll}\n" +
                            $"🎯 Total = Attack({damage}) + Crit({critRoll}) + STR({strength}) = `{totalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{creature.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Attack({damage}) + Crit({critRoll}) + STR({strength}) = `{totalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints}** HP left.";
                    }
                    break;

                // CRITICAL MISS
                case HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsCriticalMiss");

                    result =
                        $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, but critically misses!**\n" +
                        $"🎯 **[CRITICAL MISS]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 {player.Name} remains unscathed with '{player.Hitpoints}' HP!";
                    break;

                // HIT
                case HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsValidHit");

                    if (player.Hitpoints <= 0)
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);
                        result =
                            $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!\n" +
                            $"🎯 **[HIT]** Attack Roll( { state.AttackRoll} ) + {state.AbilityMod}(STR({strength}) ) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Attack{damage} + {state.AbilityMod}(STR({strength}) = `{totalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityMod}(STR({strength}) ) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Attack{damage} + {state.AbilityMod}(STR({strength}) = `{totalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsMiss");

                    result =
                        $"🗡️ **{creature.Name} attacks {player.Name}, but the {weapon.Name} bounces off!**\n" +
                        $"🎯 **[MISS]** Attack Roll( {state.AttackRoll} ) + {state.AbilityMod}(STR({strength}) ) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{player.Name}** has '{player.Hitpoints}' HP left.";
                    break;
            }

            return result;
        }
    }
    #endregion PROCESS ATTACK
}
