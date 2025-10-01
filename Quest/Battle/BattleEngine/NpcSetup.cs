using Adventure.Models.NPC;
using Adventure.Quest.Encounter;
using Adventure.Quest.Helpers;
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
            var state = BattleStateSetup.GetBattleState(userId);
            state.Npc = npc;

            // Save NPC stats to BattleState
            var RollHitpointsNPC = ChallengeRatingHelpers.GetNpcHitpoints(npc, npc.CR, userId);
            state.HitpointsAtStartNPC = RollHitpointsNPC;
            state.CurrentHitpointsNPC = RollHitpointsNPC;
            state.PreHpNPC = RollHitpointsNPC;
            state.RewardXP = ChallengeRatingHelpers.GetRewardXP(npc.CR);

            LogService.Info($"[BattleMethod.SetNpc]\n\n>NPC: {npc.Name} HP: {state.CurrentHitpointsNPC} RewardXP: {state.RewardXP}\n\n");

            if (npc.Weapons != null)
                state.NpcWeapons = GameEntityFetcher.RetrieveWeaponAttributes(npc.Weapons);

            if (npc.Armor != null)
                state.NpcArmor = GameEntityFetcher.RetrieveArmorAttributes(npc.Armor);

            EncounterBattleStepsSetup.battleStates[userId] = state;
        }
    }
}
