using Adventure.Models.Items;
using Adventure.Services;
using Adventure.Quest.Rolls;
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
using System.Collections;

namespace Adventure.Quest.Battle
{
    class ProcessBattle
    {
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
        private static (int damage, int totalDamage, List<int> rolls, int critRoll, string dice, int newHP) ProcessSuccessfulHit(ulong userId, BattleStateModel state, WeaponModel weapon, int strength, int currentHP, bool isPlayerAttacker)
        {
            // Calculate and apply damage, including critical hit or miss logic
            var (damage, totalDamage, rolls, critRoll, dice, newHP) = ProcessRollsAndDamage.RollAndApplyDamage(
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
                                $"HP Player: {state.Player.Hitpoints} VS HP NPC: {state.Creatures.Hitpoints}");
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
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: true);

            // Get combat state and both combatants
            var (state, player, creature, strength) = GetBattleParticipants(userId, playerIsAttacker: true);

            int critRoll = 0, damage = 0, totalDamage = 0;
            List<int> rolls = new();
            string dice = "";

            // If hit is successful or critical, calculate damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (damage, totalDamage, rolls, critRoll, dice, creature.Hitpoints) =
                    ProcessSuccessfulHit(userId, state, weapon, strength, creature.Hitpoints, isPlayerAttacker: true);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case ProcessRollsAndDamage. HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isCriticalHit");

                    if (creature.Hitpoints <= 0)
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Attack( {damage} ) + Critical( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{creature.Name} is defeated!**\n";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Attack( {damage} ) + Critical( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"🧟 **{creature.Name}** has **{creature.Hitpoints} HP** left.\n";
                    }
                    break;
                
                // CRITICAL MISS
                case ProcessRollsAndDamage.HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isCriticalMiss");

                    result =
                        $"🗡️ **{player.Name} attacks {creature.Name}, but critically misses!**\n----------\n" +
                        $"🎯 **[CRITICAL MISS]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n\n" +
                        $"🧟 **{creature.Name}** remains unscathed with **{creature.Hitpoints} HP**!";
                    break;

                // HIT
                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isValidHit");

                    if (creature.Hitpoints <= 0)
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( { state.AttackRoll } ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Atack( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{creature.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Attack( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"🧟 **{creature.Name}** has **{creature.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case ProcessRollsAndDamage.HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] IsMiss");

                    result =
                        $"🗡️ **{player.Name} attacks {creature.Name}, but the {weapon.Name} bounces off!**\n----------\n" +
                        $"🎯 **[MISS]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
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
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: false);
            var (state, player, creature, strength) = GetBattleParticipants(userId, playerIsAttacker: false);

            int critRoll = 0, damage = 0, totalDamage = 0;
            List<int> rolls = new();
            string dice = "";

            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (damage, totalDamage, rolls, critRoll, dice, player.Hitpoints) =
                    ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, isPlayerAttacker: false);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case ProcessRollsAndDamage.HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsCriticalHit");

                    if (player.Hitpoints <= 0)
                    {
                        result =
                            $"🗡️ **{creature.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): {critRoll}\n" +
                            $"🎯 Total = Attack( {damage} ) + Crit( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{creature.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for Attack ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Attack( {damage} ) + Crit( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints}** HP left.";
                    }
                    break;

                // CRITICAL MISS
                case ProcessRollsAndDamage.HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsCriticalMiss");

                    result =
                        $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, but critically misses!**\n----------\n" +
                        $"🎯 **[CRITICAL MISS]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 {player.Name} remains unscathed with **{player.Hitpoints}** HP!";
                    break;

                // HIT
                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsValidHit");

                    if (player.Hitpoints <= 0)
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);
                        result =
                            $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( { state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} ))  + {state.ProficiencyModifier} (CR: {state.Creatures.LevelCR} = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Attack( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} ))  + {state.ProficiencyModifier} (CR: {state.Creatures.LevelCR}) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Attack( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case ProcessRollsAndDamage.HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsMiss");

                    result =
                        $"🗡️ **{creature.Name} attacks {player.Name}, but the {weapon.Name} bounces off!**\n----------\n" +
                        $"**[MISS]** Attack Roll( { state.AttackRoll} ) + { state.AbilityModifier} (STR( { strength} )) + {state.ProficiencyModifier} (CR: {state.Creatures.LevelCR}) = [ { state.TotalRoll} ] vs AC[{ state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{player.Name}** has **{player.Hitpoints}** HP left.";
                    break;
            }

            return result;
        }
    }
    #endregion PROCESS ATTACK
}
