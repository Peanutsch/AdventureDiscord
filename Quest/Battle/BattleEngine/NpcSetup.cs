using Adventure.Models.NPC;
using Adventure.Quest.Battle.Attack;
using Adventure.Quest.Encounter;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle.BattleEngine
{
    public class NpcSetup
    {
        /// <summary>
        /// Assigns the creature for the current encounter and loads its weapons and armor.
        /// </summary>
        public static void SetupNpc(ulong userId, NpcModel npc)
        {
            var session = BattleStateSetup.GetBattleSession(userId);
            session.Context.Npc = npc;

            // Save NPC stats to BattleState
            var RollHitpointsNPC = ChallengeRatingHelpers.GetNpcHitpoints(npc, npc.CR, userId);
            session.State.HitpointsAtStartNPC = RollHitpointsNPC;
            session.State.CurrentHitpointsNPC = RollHitpointsNPC;
            session.State.PreHpNPC = RollHitpointsNPC;
            session.State.RewardXP = ChallengeRatingHelpers.GetRewardXP(npc.CR);

            LogService.Info($"[BattleMethod.SetNpc]\n\n>NPC: {npc.Name} HP: {session.State.CurrentHitpointsNPC} RewardXP: {session.State.RewardXP}\n\n");

            if (npc.Weapons != null)
                session.Context.NpcWeapons = GameEntityFetcher.RetrieveWeaponAttributes(npc.Weapons);

            if (npc.Armor != null)
                session.Context.NpcArmor = GameEntityFetcher.RetrieveArmorAttributes(npc.Armor);

            EncounterBattleStepsSetup.battleSessions[userId] = session;
        }
    }
}
