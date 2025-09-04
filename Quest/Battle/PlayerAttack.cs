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
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: true);

            // If hit is successful or critical, calculate damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, npc.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, npc.Hitpoints, isPlayerAttacker: true);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case ProcessRollsAndDamage. HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isCriticalHit");

                    if (npc.Hitpoints <= 0)
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for **damage** ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({state.Dice}): `{state.CritRoll}`\n" +
                            $"🎯 Total = Damage( {state.Damage} ) + Critical( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"💀 **{npc.Name} is defeated!**\n";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{player.Name} lands a Critical Hit on {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n" +
                            $"🎲 {player.Name} rolls for **damage** ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({state.Dice}): `{state.CritRoll}`\n" +
                            $"🎯 Total = Damage( {state.Damage} ) + Critical( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"🧟 **{npc.Name}** has **{npc.Hitpoints} HP** left.\n";
                    }
                    break;
                
                // CRITICAL MISS
                case ProcessRollsAndDamage.HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isCriticalMiss");

                    result =
                        $"🗡️ **{player.Name} attacks {npc.Name}, but critically misses!**\n----------\n" +
                        $"🎯 **[CRITICAL MISS]** Attack Roll [{state.AttackRoll}] vs AC [{state.ArmorElements.ArmorClass}]\n\n" +
                        $"🧟 **{npc.Name}** remains unscathed with **{npc.Hitpoints} HP**!";
                    break;

                // HIT
                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] isValidHit");

                    if (npc.Hitpoints <= 0)
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( { state.AttackRoll } ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for **damage** ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            $"🎯 Total = Damage( {state.Damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"💀 **{npc.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{player.Name} attacks {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [  {state.TotalRoll}  ] vs AC [  {state.ArmorElements.ArmorClass}  ]\n" +
                            $"🎲 {player.Name} rolls for **damage** ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            $"🎯 Total = Damage( {state.Damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"🧟 **{npc.Name}** has **{npc.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case ProcessRollsAndDamage.HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack] IsMiss");

                    result =
                        $"🗡️ **{player.Name} attacks {npc.Name}, but the {weapon.Name} bounces off!**\n----------\n" +
                        $"🎯 **[MISS]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.LevelCR}) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{npc.Name}** has **{npc.Hitpoints}** HP left.";
                    break;
            }

            return result;
        }
    }
}
