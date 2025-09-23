using Adventure.Data;
using Adventure.Models.NPC;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Encounter 
{
    public class EncounterRandomizer 
    {
        /*
         * How to call NpcRandomizer:
         * 
         * Low CR, but only humanoids
         * var npc1 = EncounterRandomizer.NpcRandomizer(CRWeightMode.LowCR, CreatureListPreference.Humanoids);
         * 
         * High CR, but always bestiary
         * var npc2 = EncounterRandomizer.NpcRandomizer(CRWeightMode.HighCR, CreatureListPreference.Bestiary);
         * 
         * 50/50 chance, random lijst
         * var npc3 = EncounterRandomizer.NpcRandomizer();
         */

        /// <summary>
        /// Modes for weighting NPC selection based on Challenge Rating (CR).
        /// </summary>
        public enum CRWeightMode {
            LowCR,      // Lower CR = higher chance of selection
            Balanced,   // 50/50 All NPCs have equal chance
            HighCR      // Higher CR = higher chance of selection
        }

        /// <summary>
        /// Preferences for which list of creatures to select from.
        /// </summary>
        public enum CreatureListPreference {
            Random,      // 50/50 chance between humanoids and bestiary
            Humanoids,   // Always pick from humanoids
            Bestiary     // Always pick from bestiary
        }

        /// <summary>
        /// Randomly selects a creature from either the humanoids or bestiary lists.
        /// The selection can be weighted by CR and can respect a list preference.
        /// </summary>
        /// <param name="crMode">
        /// Determines how Challenge Rating affects selection probability. Defaults to Balanced.
        /// </param>
        /// <param name="listPreference">
        /// Determines which list to pick from. Defaults to Random (50/50 chance).
        /// </param>
        /// <returns>
        /// A randomly selected NpcModel from the chosen list, or null if both lists are empty.
        /// </returns>
        public static NpcModel? NpcRandomizer(CRWeightMode crMode = CRWeightMode.Balanced, CreatureListPreference listPreference = CreatureListPreference.Random) 
            {
            try {
                // Retrieve the lists of humanoid and beast NPCs
                var humanoids = GameData.Humanoids;
                var bestiary = GameData.Bestiary;

                // Return null if both lists are null or empty
                if (ListsAreEmpty(humanoids, bestiary)) 
                {
                    LogService.Info($"[EncounterRandomizer.NpcRandomizer] ListsAreEmpty = true. Returned null");
                    return null;
                }
                    

                // Select which list to choose from based on preference and fallback rules
                var selectedList = ChooseListWeighted(humanoids!, bestiary!, listPreference);
                var selectedNpc = PickRandomNpcWeighted(selectedList, crMode);

                // Pick a random NPC from the selected list using the specified CR weighting mode
                return PickRandomNpcWeighted(selectedList, crMode);
            }
            catch (Exception ex) {
                // Log any unexpected errors
                LogService.Error($"[EncounterService.NpcRandomizer] > Error:\n{ex.Message}");
                return null;
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Checks if both humanoid and bestiary lists are null or empty.
        /// Returns true only if both lists are null or empty.
        /// </summary>
        private static bool ListsAreEmpty(List<NpcModel>? humanoids, List<NpcModel>? bestiary) {
            if ((humanoids == null || humanoids.Count == 0) &&
                (bestiary == null || bestiary.Count == 0)) 
            {
                LogService.Info($"[EncounterRandomizer.ListsAreEmpty] lists humanoids and bestiary are empty...");
                return true;
            }

            LogService.Info($"[EncounterRandomizer.ListsAreEmpty] lists humanoids and bestiary contains items...");
            return false;
        }

        /// <summary>
        /// Chooses which list to pick from based on the specified preference.
        /// Falls back to a non-empty list if the preferred one is empty.
        /// </summary>
        private static List<NpcModel> ChooseListWeighted(List<NpcModel> humanoids, List<NpcModel> bestiary, CreatureListPreference preference) {
            var random = new Random();
            List<NpcModel> selectedList;

            switch (preference) 
            {
                case CreatureListPreference.Humanoids:
                    selectedList = humanoids.Count > 0 ? humanoids : bestiary; // fallback

                    LogService.Info($"[EncounterRandomizer.ChooseListWeighted] selectedList: {selectedList}");

                    break;

                case CreatureListPreference.Bestiary:
                    selectedList = bestiary.Count > 0 ? bestiary : humanoids; // fallback

                    LogService.Info($"[EncounterRandomizer.ChooseListWeighted] selectedList: {selectedList}");

                    break;

                case CreatureListPreference.Random:
                    if (random.NextDouble() < 0.5 && humanoids.Count > 0) 
                    {
                        LogService.Info($"[EncounterRandomizer.ChooseListWeighted] return selectedList = humanoids");

                        selectedList = humanoids;
                    }
                    else if (bestiary.Count > 0) 
                    {
                        LogService.Info($"[EncounterRandomizer.ChooseListWeighted] return selectedList = bestiary");
                        selectedList = bestiary;
                    }
                    else 
                    {
                        LogService.Info($"[EncounterRandomizer.ChooseListWeighted] FALLBACK!!! return selectedList = humanoids");
                        selectedList = humanoids; // fallback
                    }
                    break;

                default:
                    selectedList = humanoids.Count > 0 ? humanoids : bestiary; // fallback
                    LogService.Info($"[EncounterRandomizer.ChooseListWeighted] DEFAULT FALLBACK!!! return selectedList = {selectedList}");
                    break;
            }

            return selectedList;
        }

        /// <summary>
        /// Picks a random NPC from a list using CRWeightMode.
        /// LowCR = higher chance for weaker NPCs,
        /// Balanced = equal chance,
        /// HighCR = higher chance for stronger NPCs.
        /// </summary>
        private static NpcModel PickRandomNpcWeighted(List<NpcModel> list, CRWeightMode mode) 
        {
            var random = new Random();
            List<double> weights;

            // Generate weights based on CR mode using classic switch statement
            switch (mode) {
                case CRWeightMode.LowCR:
                    weights = list.Select(npc => 1.0 / Math.Max(npc.CR, 0.01)).ToList();
                    break;

                case CRWeightMode.Balanced:
                    weights = list.Select(npc => 1.0).ToList();
                    break;

                case CRWeightMode.HighCR:
                    weights = list.Select(npc => Math.Max(npc.CR, 0.01)).ToList();
                    break;

                default:
                    weights = list.Select(npc => 1.0).ToList();
                    break;
            }

            // Sum total weight for probability range
            double totalWeight = weights.Sum();
            double roll = random.NextDouble() * totalWeight;

            // Determine which NPC falls under the rolled weight
            double cumulative = 0;
            for (int i = 0; i < list.Count; i++) {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return list[i];
            }

            // Fallback: return a truly random NPC if something goes wrong
            return list[random.Next(list.Count)];
        }
    }
}
