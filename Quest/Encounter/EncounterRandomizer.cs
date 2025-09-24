using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.NPC;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Encounter
{
    /// <summary>
    /// Provides functionality to randomly select NPCs for encounters.
    /// NPCs can be selected from humanoid or bestiary lists, and 
    /// the selection can be weighted by challenge rating (CR).
    /// </summary>
    public class EncounterRandomizer
    {
        /*
         * Usage Examples:
         *
         * Low CR preference, but only from humanoids:
         * var npc = EncounterRandomizer.NpcRandomizer(CRWeightMode.LowCR, CreatureListPreference.Humanoids);
         *
         * Hig:h CR preference, but always from bestiary
         * var npc = EncounterRandomizer.NpcRandomizer(CRWeightMode.HighCR, CreatureListPreference.Bestiary);
         *
         * Balanced CR, 50/50 chance between humanoids and bestiary:
         * var npc = EncounterRandomizer.NpcRandomizer();
         */

        #region NPC RANDOMIZER
        /// <summary>
        /// Randomly selects an NPC from the humanoids or bestiary list.
        /// The chosen list and weighting can be influenced by parameters.
        /// </summary>
        /// <param name="crMode">
        /// Determines how Challenge Rating influences selection probability.
        /// Defaults to <see cref="CRWeightMode.Balanced"/>.
        /// </param>
        /// <param name="listPreference">
        /// Determines which creature list to select from.
        /// Defaults to <see cref="CreatureListPreference.Random"/>.
        /// </param>
        /// <returns>
        /// A randomly selected <see cref="NpcModel"/>, or <c>null</c> if no creatures are available.
        /// </returns>
        public static NpcModel? NpcRandomizer(CRWeightMode crMode = CRWeightMode.Balanced, CreatureListPreference listPreference = CreatureListPreference.Random)
        {
            try
            {
                // Ensure lists are loaded and not null
                var (humanoids, bestiary) = LoadLists();

                // Choose which list to use (humanoids or bestiary) depending on preference
                var selectedList = ChooseListWeighted(humanoids, bestiary, listPreference);

                // Select a random NPC from the chosen list, applying CR weighting
                var pickedNpcWeighted = PickRandomNpcWeighted(selectedList, crMode);

                LogService.Info($"[EncounterRandomizer.NpcRandomizer]\n\n> Picked NPC: {pickedNpcWeighted!.Name}\n\n");
                return pickedNpcWeighted;
            }
            catch (Exception ex)
            {
                // Log unexpected errors and return null as a safe fallback
                LogService.Error($"[EncounterService.NpcRandomizer] > Error:\n{ex.Message}");
                return null;
            }
        }
        #endregion NPC RANDOMIZER

        #region HELPER METHODS
        private static readonly Random _random = new Random();

        /// <summary>
        /// Modes for weighting NPC selection based on Challenge Rating (CR).
        /// </summary>
        public enum CRWeightMode
        {
            LowCR,      // Lower CR = higher chance of being selected
            Balanced,   // Equal chance for all NPCs regardless of CR
            HighCR      // Higher CR = higher chance of being selected
        }

        /// <summary>
        /// Preferences for which list of creatures to select from.
        /// </summary>
        public enum CreatureListPreference
        {
            Random,     // 50/50 chance between humanoids and bestiary
            Humanoids,  // Always select from humanoids
            Bestiary    // Always select from bestiary
        }

        /// <summary>
        /// Ensures humanoids and bestiary lists are available.
        /// If both lists are null or empty, reload them using the loaders.
        /// </summary>
        /// <returns>
        /// A tuple containing the humanoid and bestiary lists (never null).
        /// </returns>
        private static (List<NpcModel> humanoids, List<NpcModel> bestiary) LoadLists()
        {
            var humanoids = GameData.Humanoids;
            var bestiary = GameData.Bestiary;

            // If both are empty → reload from loaders
            if ((humanoids == null || humanoids.Count == 0) &&
                (bestiary == null || bestiary.Count == 0))
            {
                LogService.Info("[EncounterRandomizer.LoadLists] Both lists empty → Reloading...");

                GameData.Humanoids = HumanoidLoader.Load();
                GameData.Bestiary = BestiaryLoader.Load();

                humanoids = GameData.Humanoids ?? new List<NpcModel>();
                bestiary = GameData.Bestiary ?? new List<NpcModel>();
            }

            // Always return non-null lists
            return (humanoids ?? new List<NpcModel>(), bestiary ?? new List<NpcModel>());
        }

        /// <summary>
        /// Chooses which list of NPCs to select from based on preference and fallback rules.
        /// If the preferred list is empty, falls back to the other list.
        /// </summary>
        /// <param name="humanoids">The humanoid list (may be empty).</param>
        /// <param name="bestiary">The bestiary list (may be empty).</param>
        /// <param name="preference">The creature list preference.</param>
        /// <returns>
        /// A list of NPCs to select from (may be empty if both lists are empty).
        /// </returns>
        private static List<NpcModel> ChooseListWeighted(List<NpcModel> humanoids, List<NpcModel> bestiary, CreatureListPreference preference)
        {
            // If both lists are empty → return empty list
            if (humanoids.Count == 0 && bestiary.Count == 0)
            {
                LogService.Info("[EncounterRandomizer.ChooseListWeighted] Both lists empty → returning empty list.");
                return new List<NpcModel>();
            }

            List<NpcModel> selectedList;

            switch (preference)
            {
                case CreatureListPreference.Humanoids:
                    // Prefer humanoids, fallback to bestiary if empty
                    selectedList = humanoids.Count > 0 ? humanoids : bestiary;
                    break;

                case CreatureListPreference.Bestiary:
                    // Prefer bestiary, fallback to humanoids if empty
                    selectedList = bestiary.Count > 0 ? bestiary : humanoids;
                    break;

                case CreatureListPreference.Random:
                    // 50/50 split, but only if lists are not empty
                    if (_random.NextDouble() < 0.5 && humanoids.Count > 0)
                        selectedList = humanoids;
                    else if (bestiary.Count > 0)
                        selectedList = bestiary;
                    else
                        selectedList = humanoids; // fallback (still possibly empty)
                    break;

                default:
                    // Default fallback: use humanoids if possible, otherwise bestiary
                    selectedList = humanoids.Count > 0 ? humanoids : bestiary;
                    break;
            }

            LogService.Info($"[EncounterRandomizer.ChooseListWeighted] Selected list contains {selectedList.Count} entries.");
            return selectedList;
        }

        /// <summary>
        /// Picks a random NPC from a list based on the selected CR weighting mode.
        /// </summary>
        /// <param name="list">The list of NPCs to choose from.</param>
        /// <param name="mode">The CR weighting mode.</param>
        /// <returns>
        /// A randomly selected <see cref="NpcModel"/>, or <c>null</c> if the list is empty.
        /// </returns>
        private static NpcModel? PickRandomNpcWeighted(List<NpcModel> list, CRWeightMode mode)
        {
            if (list == null || list.Count == 0)
            {
                LogService.Info("[EncounterRandomizer.PickRandomNpcWeighted] Empty list → returning null.");
                return null;
            }

            List<double> weights;

            // Assign weights depending on the CR weighting mode
            switch (mode)
            {
                case CRWeightMode.LowCR:
                    // Inverse weight: lower CR gets higher weight
                    weights = list.Select(npc => 1.0 / Math.Max(npc.CR, 0.01)).ToList();
                    break;

                case CRWeightMode.Balanced:
                    // All NPCs have equal chance
                    weights = list.Select(_ => 1.0).ToList();
                    break;

                case CRWeightMode.HighCR:
                    // Direct weight: higher CR gets higher weight
                    weights = list.Select(npc => Math.Max(npc.CR, 0.01)).ToList();
                    break;

                default:
                    // Default to equal weighting
                    weights = list.Select(_ => 1.0).ToList();
                    break;
            }

            // Randomly roll against the total weight
            double totalWeight = weights.Sum();
            double roll = _random.NextDouble() * totalWeight;

            double cumulative = 0;
            for (int i = 0; i < list.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return list[i];
            }

            // Fallback: pure random choice
            return list[_random.Next(list.Count)];
        }
        #endregion HELPER METHODS
    }
}
