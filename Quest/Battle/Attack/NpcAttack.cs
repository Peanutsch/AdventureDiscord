using Adventure.Models.Items;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Services;

namespace Adventure.Quest.Battle.Attack
{
    class NpcAttack
    {
        /// <summary>
        /// Processes the NPC's attack during battle, calculates hit or miss based on dice roll and strength modifier,
        /// handles critical hits/misses, and returns a descriptive battle message.
        /// </summary>
        public static string ProcessNpcAttack(ulong userId, WeaponModel weapon)
        {
            var hitResult = ProcessRollsAndDamage.ValidateHit(userId, isPlayerAttacker: false);
            var (state, player, npc, strength) = GetBattleStateData.GetBattleParticipants(userId, playerIsAttacker: false);
            var npcCR = ChallengeRatingHelpers.DisplayCR(npc.CR);

            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, player.Hitpoints) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, player.Hitpoints, isPlayerAttacker: false);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case ProcessRollsAndDamage.HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack]\n\n>Result attack: IsCriticalHit\n\n");

                    if (player.Hitpoints <= 0)
                    {
                        result =
                            $"🗡️ **[CRITICAL HIT] {npc.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n\n" +
                            //$"🎯 Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                            //$"🎲 Damage ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            //$"💥 Critical Damage ({state.Dice}): {state.CritRoll}\n" +
                            //$"🎯 Total = Damage ( {state.Damage} ) + Critical Damage ( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        result =
                            $"🗡️ **[CRITICAL HIT] {npc.Name} lands a Critical Hit on {player.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n\n" +
                            //$"🎯 Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                            //$"🎲 Damage ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            //$"💥 Critical Damage ({state.Dice}): `{state.CritRoll}`\n" +
                            //$"🎯 Total = Damage ( {state.Damage} ) + Critical Damage ( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints}** HP left.";
                    }
                    break;

                // CRITICAL MISS
                case ProcessRollsAndDamage.HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack]\n\n> Result attack: IsCriticalMiss\n\n");

                    result =
                        $"🗡️ **[MISS] {npc.Name} attacks {player.Name} with {weapon.Name}, but critically misses!**\n\n" +
                        //$"🎯 Attack Roll [ {state.AttackRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 {player.Name} remains unscathed with **{player.Hitpoints}** HP!";
                    break;

                // HIT
                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack]\n\n> Result attack: IsValidHit\n\n");

                    if (player.Hitpoints <= 0)
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepEndBattle);
                        result =
                            $"🗡️ **[HIT] {npc.Name} attacks {player.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!\n\n" +
                            //$"🎯 Attack Roll ( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} ))  + {state.ProficiencyModifier} (CR: {npcCR} = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                            //$"🎲 Damage ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            //$"🎯 Total = Damage ( {state.Damage} ) + {state.AbilityModifier} (STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"💀 **{player.Name} is defeated!**";
                    }
                    else
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepPostBattle);
                        result =
                            $"🗡️ **[HIT] {npc.Name} attacks {player.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n\n" +
                            //$"🎯 Attack Roll ( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} ))  + {state.ProficiencyModifier} (CR: {npcCR}) = [ {state.TotalRoll} ] vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                            //$"🎲 Damage ({state.Dice}): `{string.Join(", ", state.Rolls)}`\n" +
                            //$"🎯 Total = Damage ( {state.Damage} ) + {state.AbilityModifier} (STR( {strength} )) = `{state.TotalDamage}`\n\n" +
                            $"🧟 **{player.Name}** has **{player.Hitpoints} HP** left.";
                    }
                    break;

                // MISS
                case ProcessRollsAndDamage.HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessCreatureAttack]\n\n> Result attack: IsMiss\n\n");

                    result =
                        $"🗡️ **[MISS] {npc.Name} attacks {player.Name}, but the {weapon.Name} bounces off!**\n\n" +
                        //$"🎯 Attack Roll ( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (CR: {npcCR}) = [ {state.TotalRoll} ] vs AC[{state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{player.Name}** has **{player.Hitpoints}** HP left.";
                    break;
            }

            return result;
        }
    }
}
