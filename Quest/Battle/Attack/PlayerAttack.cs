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
            (string battleLog, BattleSession session) = AttackProcessor.ProcessAttack(userId, weapon, true);

            // If NPC is dead → end battle + reward XP
            if (session.State.CurrentHitpointsNPC <= 0)
            {
                EncounterBattleStepsSetup.SetStep(userId, BattleStep.EndBattle);
                session.State.EmbedColor = Color.Purple;

                // Get multiplayer damage distribution from encounter tracker
                string? encounterTileId = session.State.EncounterTileId;
                Dictionary<ulong, int> damageRatios = new Dictionary<ulong, int>();
                bool isFirstVictory = true;

                if (!string.IsNullOrEmpty(encounterTileId))
                {
                    damageRatios = Adventure.Services.ActiveEncounterTracker.GetDamageRatios(encounterTileId);

                    // If damage ratios exist (multiplayer), use them; otherwise fallback to single player
                    if (damageRatios.ContainsKey(userId))
                    {
                        session.State.RatioDamageDealt = damageRatios[userId];
                    }
                    else
                    {
                        // Fallback for single player or if tracking failed
                        int totalDamageDealt = session.State.HitpointsAtStartNPC - session.State.CurrentHitpointsNPC;
                        session.State.RatioDamageDealt = session.State.HitpointsAtStartNPC > 0
                            ? (int)Math.Round((double)totalDamageDealt / session.State.HitpointsAtStartNPC * 100)
                            : 100;
                    }

                    session.State.RatioDamageDealt = Math.Clamp(session.State.RatioDamageDealt, 0, 100);

                    // Thread-safe removal - only first player gets true (prevents duplicate XP awards)
                    isFirstVictory = Adventure.Services.ActiveEncounterTracker.TryRemoveEncounter(encounterTileId);
                }
                else
                {
                    // No encounter tile (shouldn't happen, but fallback)
                    int totalDamageDealt = session.State.HitpointsAtStartNPC - session.State.CurrentHitpointsNPC;
                    session.State.RatioDamageDealt = session.State.HitpointsAtStartNPC > 0
                        ? (int)Math.Round((double)totalDamageDealt / session.State.HitpointsAtStartNPC * 100)
                        : 100;
                    session.State.RatioDamageDealt = Math.Clamp(session.State.RatioDamageDealt, 0, 100);
                }

                // Only award XP if this is the first victory (prevents duplicate XP from concurrent attacks)
                if (isFirstVictory)
                {
                    // Calculate XP for THIS player based on their damage ratio
                    int rewardXP = ChallengeRatingHelpers.GetRewardXP(session.Context.Npc.CR);
                    (bool leveledUp, int oldLevel, int newLevel) = ProcessSuccesAttack.ProcessXPReward(rewardXP, session);
                    session.State.PlayerLeveledUp = leveledUp;

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
                            var participantSession = BattleStateSetup.GetBattleSession(participantId);
                            if (participantSession != null)
                            {
                                participantSession.State.RatioDamageDealt = participantRatio;
                                ProcessSuccesAttack.ProcessXPReward(rewardXP, participantSession);

                                // End battle for this participant too
                                EncounterBattleStepsSetup.SetStep(participantId, BattleStep.EndBattle);

                                LogService.Info($"[PlayerAttack] Awarded {participantSession.State.RewardXP} XP to participant {participantId} ({participantRatio}% damage)");
                            }
                        }
                    }

                    // Generate battle log for victory and XP reward
                    battleLog += $"\n\n💀 **VICTORY!!! {session.Context.Npc.Name} is defeated after {session.State.RoundCounter} {UseOfS(session.State.RoundCounter)}!**";
                    battleLog += $"\n🏆 **{session.Context.Player.Name}** gains **{session.State.RewardXP} XP** (Total: {session.State.NewTotalXP} XP)";

                    // Show multiplayer contribution if applicable
                    if (damageRatios.Count > 1)
                    {
                        battleLog += $"\n⚔️ **Team Effort!** You contributed **{session.State.RatioDamageDealt}%** of total damage";
                    }

                    if (leveledUp)
                        battleLog += $"\n\n✨ **LEVEL UP!** {session.Context.Player.Name} advanced from **Level {oldLevel} → Level {newLevel}**!";
                }
                else
                {
                    // Another player already claimed victory (concurrent attack)
                    battleLog += $"\n\n⚔️ **{session.Context.Npc.Name} was already defeated by another player!**";
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