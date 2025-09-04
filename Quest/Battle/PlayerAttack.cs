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
    class PlayerAttack
    {
        /// <summary>
        /// Processes the player's attack during battle, calculates hit or miss based on dice roll and strength modifier,
        /// handles critical hits/misses, and returns a descriptive battle message.
        /// </summary>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            // Determine hit result from attack roll (miss, hit, critical, etc.)
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: true);

            // Get combat state and both combatants
            var (state, player, creature, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: true);

            int critRoll = 0, damage = 0, totalDamage = 0;
            List<int> rolls = new();
            string dice = "";

            // If hit is successful or critical, calculate damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (damage, totalDamage, rolls, critRoll, dice, creature.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, creature.Hitpoints, isPlayerAttacker: true);
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
                            $"🎲 {player.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Damage( {damage} ) + Critical( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{creature.Name} is defeated!**\n";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Damage( {damage} ) + Critical( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
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
                            $"🎲 {player.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Damage( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{creature.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {creature.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Damage( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
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
    }
}
