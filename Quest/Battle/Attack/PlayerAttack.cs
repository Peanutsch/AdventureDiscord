using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Process;
using Adventure.Services;
using Discord;

namespace Adventure.Quest.Battle.Attack
{
    class PlayerAttack
    {
        /// <summary>
        /// Processes the player's attack on the NPC, updates the battle state, and generates the battle log. 
        /// Handles the end of the battle and rewards XP to the player if the NPC is defeated.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            // // Get attack data from AttackProcessor
            (string battleLog, BattleStateModel state) = AttackProcessor.ProcessAttack(userId, weapon, true);

            // If NPC is dead → end battle + reward XP
            if (state.CurrentHitpointsNPC <= 0)
            {
                EncounterBattleStepsSetup.SetStep(userId, BattleStep.EndBattle);
                state.EmbedColor = Color.Purple;

                // Get multiplayer damage distribution from encounter tracker
                string? encounterTileId = state.EncounterTileId;
                Dictionary<ulong, int> damageRatios = new Dictionary<ulong, int>();
                bool isFirstVictory = true;

                if (!string.IsNullOrEmpty(encounterTileId))
                {
                    damageRatios = Adventure.Services.ActiveEncounterTracker.GetDamageRatios(encounterTileId);

                    // If damage ratios exist (multiplayer), use them; otherwise fallback to single player
                    if (damageRatios.ContainsKey(userId))
                    {
                        state.RatioDamageDealt = damageRatios[userId];
                    }
                    else
                    {
                        // Fallback for single player or if tracking failed
                        int totalDamageDealt = state.HitpointsAtStartNPC - state.CurrentHitpointsNPC;
                        state.RatioDamageDealt = state.HitpointsAtStartNPC > 0
                            ? (int)Math.Round((double)totalDamageDealt / state.HitpointsAtStartNPC * 100)
                            : 100;
                    }

                    state.RatioDamageDealt = Math.Clamp(state.RatioDamageDealt, 0, 100);

                    // Thread-safe removal - only first player gets true (prevents duplicate XP awards)
                    isFirstVictory = Adventure.Services.ActiveEncounterTracker.TryRemoveEncounter(encounterTileId);
                }
                else
                {
                    // No encounter tile (shouldn't happen, but fallback)
                    int totalDamageDealt = state.HitpointsAtStartNPC - state.CurrentHitpointsNPC;
                    state.RatioDamageDealt = state.HitpointsAtStartNPC > 0
                        ? (int)Math.Round((double)totalDamageDealt / state.HitpointsAtStartNPC * 100)
                        : 100;
                    state.RatioDamageDealt = Math.Clamp(state.RatioDamageDealt, 0, 100);
                }

                // Only award XP if this is the first victory (prevents duplicate XP from concurrent attacks)
                if (isFirstVictory)
                {
                    // Calculate XP for THIS player based on their damage ratio
                    int rewardXP = ChallengeRatingHelpers.GetRewardXP(state.Npc.CR);
                    (bool leveledUp, int oldLevel, int newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, state);
                    state.PlayerLeveledUp = leveledUp;

                    // Award XP to all other participating players in multiplayer
                    if (damageRatios.Count > 1)
                    {
                        foreach (var kvp in damageRatios)
                        {
                            ulong participantId = kvp.Key;
                            int participantRatio = kvp.Value;

                            // Skip current player (already processed above)
                            if (participantId == userId)
                                continue;

                            // Award XP to other participants
                            var participantState = BattleStateSetup.GetBattleState(participantId);
                            if (participantState != null)
                            {
                                participantState.RatioDamageDealt = participantRatio;
                                ProcessSuccesAttack.ProcessXPReward(rewardXP, participantState);

                                // End battle for this participant too
                                EncounterBattleStepsSetup.SetStep(participantId, BattleStep.EndBattle);

                                LogService.Info($"[PlayerAttack] Awarded {participantState.RewardXP} XP to participant {participantId} ({participantRatio}% damage)");
                            }
                        }
                    }

                    // Generate battle log for victory and XP reward
                    battleLog += $"\n\n💀 **VICTORY!!! {state.Npc.Name} is defeated after {state.RoundCounter} {UseOfS(state.RoundCounter)}!**";
                    battleLog += $"\n🏆 **{state.Player.Name}** gains **{state.RewardXP} XP** (Total: {state.NewTotalXP} XP)";

                    // Show multiplayer contribution if applicable
                    if (damageRatios.Count > 1)
                    {
                        battleLog += $"\n⚔️ **Team Effort!** You contributed **{state.RatioDamageDealt}%** of total damage";
                    }

                    if (leveledUp)
                        battleLog += $"\n\n✨ **LEVEL UP!** {state.Player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
                }
                else
                {
                    // Another player already claimed victory (concurrent attack)
                    battleLog += $"\n\n⚔️ **{state.Npc.Name} was already defeated by another player!**";
                    battleLog += $"\n🏆 XP has already been distributed to all participants";
                }
            }
            else
            {
                // If NPC is still alive → set next step to PostBattle for NPC's turn
                EncounterBattleStepsSetup.SetStep(userId, BattleStep.PostBattle);
            }

            return battleLog;
        }

        // Helper method to determine correct pluralization of "round(s)"
        public static string UseOfS(int round)
        {
            if (round > 0)
            {
                return "rounds";
            }
            else
            {
                return "round";
            }
        }
    }

}