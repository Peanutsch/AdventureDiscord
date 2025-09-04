using Adventure.Models.Items;
using Adventure.Quest.Rolls;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle
{
    class NpcAttack
    {
        /// <summary>
        /// Processes the NPC's attack during battle, calculates hit or miss based on dice roll and strength modifier,
        /// handles critical hits/misses, and returns a descriptive battle message.
        /// </summary>
        public static string ProcessCreatureAttack(ulong userId, WeaponModel weapon)
        {
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: false);
            var (state, player, creature, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: false);

            int critRoll = 0, damage = 0, totalDamage = 0;
            List<int> rolls = new();
            string dice = "";

            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (damage, totalDamage, rolls, critRoll, dice, player.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, isPlayerAttacker: false);
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
                            $"🎲 {creature.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): {critRoll}\n" +
                            $"🎯 Total = Damage( {damage} ) + Crit( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        result =
                            $"🗡️ **{creature.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[CRITICAL HIT]** Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"💥 Critical Hit extra roll ({dice}): `{critRoll}`\n" +
                            $"🎯 Total = Damage( {damage} ) + Crit( {critRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{totalDamage}`\n\n" +
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
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} ))  + {state.ProficiencyModifier} (CR: {state.Creatures.LevelCR} = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Damage( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                        result =
                            $"🗡️ **{creature.Name} attacks {player.Name} with {weapon.Name}, dealing `{totalDamage}` damage!**\n----------\n" +
                            $"🎯 **[HIT]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} ))  + {state.ProficiencyModifier} (CR: {state.Creatures.LevelCR}) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n" +
                            $"🎲 {creature.Name} rolls for **damage** ({dice}): `{string.Join(", ", rolls)}`\n" +
                            $"🎯 Total = Damage( {damage} ) + {state.AbilityModifier}(STR( {strength} )) = `{totalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case ProcessRollsAndDamage.HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack] IsMiss");

                    result =
                        $"🗡️ **{creature.Name} attacks {player.Name}, but the {weapon.Name} bounces off!**\n----------\n" +
                        $"🎯 **[MISS]** Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (CR: {state.Creatures.LevelCR}) = [ {state.TotalRoll} ] vs AC[{state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{player.Name}** has **{player.Hitpoints}** HP left.";
                    break;
            }

            return result;
        }
    }
}
