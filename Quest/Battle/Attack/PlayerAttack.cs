using Adventure.Models.Items;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Adventure.Services;
using Discord;
using System.Numerics;

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
                    if (state.CurrentHitpointsNPC <= 0)
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepEndBattle);

                        (bool leveledUp, int oldLevel, int newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);

                        result =
                            $"🗡️ **[CRITICAL HIT] {player.Name} lands a [Critical Hit] on {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n" +
                            $"🎯 Attack Roll [{state.AttackRoll}]\n" +
                            $"🎲 Damage ({state.Dice}): **{string.Join(", ", state.Rolls)}**\n" +
                            $"💥 Critical Damage ({state.Dice}): **{state.CritRoll}**\n" +
                            $"🎯 Total = Damage ( {state.Damage} ) + Critical Damage ( {state.CritRoll} ) + {state.AbilityModifier} (STR( {strength} )) = **{state.TotalDamage}**\n\n" +
                            $"💀 **{npc.Name} is defeated!**\n\n🏆 **{player.Name}** is rewarded with **{state.RewardXP} XP** and has now a total of **{state.NewTotalXP} XP**!";

                            if (leveledUp)
                            {
                                result += $"\n\n✨ **LEVEL UP!** {player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
                            }
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
                    result =
                        $"🗡️ **[MISS] {player.Name} attacks {npc.Name}, but critically misses!**\n" +
                        $"🎯 Attack Roll [{state.AttackRoll}]\n\n" +
                        $"🧟 **{npc.Name}** remains unscathed!"; 
                    break;

                // HIT
                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    if (state.CurrentHitpointsNPC <= 0)
                    {
                        BattleMethods.SetStep(userId, BattleMethods.StepEndBattle);

                        var (leveledUp, oldLevel, newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);

                        result =
                            $"🗡️ **[HIT] {player.Name} attacks {npc.Name} with {weapon.Name}, dealing `{state.TotalDamage}` damage!**\n" +
                            $"🎯 Attack Roll( {state.AttackRoll} ) + {state.AbilityModifier} (STR( {strength} )) + {state.ProficiencyModifier} (Level: {oldLevel}) = **{state.TotalRoll}**\n" +
                            $"🎲 Damage ({state.Dice}): ** {string.Join(", ", state.Rolls)} **\n" +
                            $"🎯 Total = Damage ( {state.Damage} ) + {state.AbilityModifier} (STR( {strength} )) = **{state.TotalDamage}**\n\n" +
                            $"💀 **{npc.Name} is defeated!**\n\n🏆 **{player.Name}** is rewarded with **{state.RewardXP} XP** and has now a total of **{state.NewTotalXP} XP**!";

                            if (leveledUp)
                            {
                                result += $"\n\n✨ **LEVEL UP!** {player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
                            }
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
