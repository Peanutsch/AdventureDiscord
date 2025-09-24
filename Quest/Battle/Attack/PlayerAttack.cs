using Adventure.Models.Items;
using Adventure.Services;
using Adventure.Quest.Rolls;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Helpers;
using Adventure.Quest.Battle.BattleEngine;

namespace Adventure.Quest.Battle.Attack
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

            // Get NPC related XP reward
            var rewardXP = ChallengeRatingHelpers.GetRewardXP(state.Npc.CR);

            // If hit is successful or critical, calculate damage
            if (hitResult == ProcessRollsAndDamage.HitResult.IsValidHit || hitResult == ProcessRollsAndDamage.HitResult.IsCriticalHit)
            {
                (state.Damage, state.TotalDamage, state.Rolls, state.CritRoll, state.Dice, state.CurrentHitpointsNPC) =
                    ProcessSuccesAttack.ProcessSuccessfulHit(userId, state, weapon, strength, state.CurrentHitpointsNPC, isPlayerAttacker: true);
            }

            string result;

            switch (hitResult)
            {
                // CRITICAL HIT
                case ProcessRollsAndDamage. HitResult.IsCriticalHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack]\n\n>Result attack: isCriticalHit\n\n");

                    if (state.CurrentHitpointsNPC <= 0)
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepEndBattle);
                        ProcessSuccesAttack.ProcessSaveXPReward(rewardXP, state);

                        result =
                            $"🗡️ **[CRITICAL HIT] {player.Name} lands a [Critical Hit] on {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n" +
                            $"🎯 Attack Roll [{state.AttackRoll}]\n" +
                            $"🎲 Damage ({state.Dice}): **{string.Join(", ", state.Rolls)}**\n" +
                            $"💥 Critical Damage ({state.Dice}): **{state.CritRoll}**\n" +
                            $"🎯 Total = Damage ( {state.Damage} ) + Critical Damage ( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = **{state.TotalDamage}**\n\n" +
                            $"💀 **{npc.Name} is defeated!**\n\n**{player.Name}** is rewarded with **{state.RewardXP} XP** and has now a total of **{state.NewTotalXP} XP**!";
                    }
                    else
                    {
                        result =
                            $"🗡️ **[CRITICAL HIT] {player.Name} lands a [Critical Hit] on {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n" +
                            $"🎯 Attack Roll [{state.AttackRoll}]\n" +
                            $"🎲 Damage ({state.Dice}): **{string.Join(", ", state.Rolls)}**\n" +
                            $"💥 Critical Damage ({state.Dice}): **{state.CritRoll}**\n" +
                            $"🎯 Total = Damage ( {state.Damage} ) + Critical Damage ( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = **{state.TotalDamage}**\n\n" +
                            $"🧟 Some text with damage degree like scratched and severe wound...\n"; 
                    }
                    break;
                
                // CRITICAL MISS
                case ProcessRollsAndDamage.HitResult.IsCriticalMiss:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack]\n\n> Result attack: isCriticalMiss\n\n");

                    result =
                        $"🗡️ **[MISS] {player.Name} attacks {npc.Name}, but critically misses!**\n" +
                        $"🎯 Attack Roll [{state.AttackRoll}]\n\n" +
                        $"🧟 **{npc.Name}** remains unscathed!"; 
                    break;

                // HIT
                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack]\n\n>Result attack: isValidHit\n\n");

                    if (state.CurrentHitpointsNPC <= 0)
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepEndBattle);
                        ProcessSuccesAttack.ProcessSaveXPReward(rewardXP, state);

                        result =
                            $"🗡️ **[HIT] {player.Name} attacks {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n" + 
                            $"🎯 Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.Level}) = **{state.TotalRoll}**\n" +
                            $"🎲 Damage ({state.Dice}): ** {string.Join(", ", state.Rolls)} **\n" +
                            $"🎯 Total = Damage ( {state.Damage} ) + {state.AbilityModifier} (STR( {strength} )) = **{state.TotalDamage}**\n\n" +
                            $"💀 **{npc.Name} is defeated!**\n\n**{player.Name}** is rewarded with **{state.RewardXP} XP** and has now a total of **{state.NewTotalXP} XP**!";
                    }
                    else
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepPostBattle);
                        result =
                            $"🗡️ **[HIT] {player.Name} attacks {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n" +
                            $"🎯 Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.Level}) = **{state.TotalRoll}**\n" +
                            $"🎲 Damage ({state.Dice}): **{string.Join(", ", state.Rolls)}**\n" +
                            $"🎯 Total = Damage ( {state.Damage} ) + {state.AbilityModifier} (STR( {strength} )) = **{state.TotalDamage}**\n\n" +
                            $"🧟 Some text with damage degree like just scratched or severe wound...\n"; 
                    }
                    break;

                // MISS
                case ProcessRollsAndDamage.HitResult.IsMiss:
                default:
                    LogService.Info("[BattleEngineHelpers.ProcessPlayerAttack]\n\n>Result attack: IsMiss\n\n");

                    result =
                        $"🗡️ **[MISS] {player.Name} attacks {npc.Name}, but the {weapon.Name} bounces off!**\n" +
                        $"🎯 Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {state.Player.Level}) = **{state.TotalRoll}**\n\n" +// vs AC [ {state.ArmorElements.ArmorClass} ]\n\n" +
                        $"🧟 **{npc.Name}** remains unscathed!";
                    break;
            }

            return result;
        }
    }
}
