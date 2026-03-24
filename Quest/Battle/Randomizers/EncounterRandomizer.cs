using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.NPC;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.Quest.Battle.Randomizers
{
    /// <summary>
    /// Provides intelligent random NPC selection for encounters with flexible weighting options.
    /// 
    /// This class handles:
    /// - Randomly selecting NPCs from humanoid or bestiary collections
    /// - Applying Challenge Rating (CR) preferences (prefer low, high, or balanced)
    /// - Weighted list selection (humanoids vs bestiary with configurable probability)
    /// - Error handling and logging for debugging
    /// 
    /// Core Features:
    /// - CR Weight Modes: LowCR (easy), Balanced (neutral), HighCR (hard)
    /// - List Preferences: Random (50/50), Humanoids (always), Bestiary (always)
    /// - Fully randomized or weighted selection based on game design needs
    /// 
    /// <remarks>
    /// Usage Example:
    /// 
    /// // Random balanced encounter
    /// var npc = EncounterRandomizer.NpcRandomizer();
    /// 
    /// // Hard encounter from bestiary only
    /// var npc = EncounterRandomizer.NpcRandomizer(
    ///     CRWeightMode.HighCR, 
    ///     CreatureListPreference.Bestiary);
    /// 
    /// // Easy humanoid encounter
    /// var npc = EncounterRandomizer.NpcRandomizer(
    ///     CRWeightMode.LowCR,
    ///     CreatureListPreference.Humanoids);
    /// 
    /// Thread Safety: Uses static Random instance. Consider thread-safety 
    /// if called from multiple threads simultaneously.
    /// </remarks>
    /// </summary>
    public class EncounterRandomizer
    {
        /*
           USAGE EXAMPLES:

           Low CR preference, but only from humanoids:
           var npc = EncounterRandomizer.NpcRandomizer(CRWeightMode.LowCR, CreatureListPreference.Humanoids);

           High CR preference, but always from bestiary:
           var npc = EncounterRandomizer.NpcRandomizer(CRWeightMode.HighCR, CreatureListPreference.Bestiary);

           Balanced CR, 50/50 chance between humanoids and bestiary:
           var npc = EncounterRandomizer.NpcRandomizer();
        */

        #region === NPC RANDOMIZER ===

        /// <summary>
        /// Randomly selects an NPC from the humanoids or bestiary list with optional weighting.
        /// 
        /// This is the primary entry point for NPC selection. It handles:
        /// 1. Loading NPC lists from data
        /// 2. Choosing between humanoids/bestiary based on preference
        /// 3. Selecting a random NPC with optional CR weighting
        /// 4. Error handling and logging
        /// 
        /// The method is robust and will return null gracefully if NPCs cannot be loaded.
        /// </summary>
        /// <param name="crMode">
        /// Determines how Challenge Rating influences selection probability.
        /// - LowCR: Lower CR NPCs are more likely to be selected (easier encounters)
        /// - Balanced: All NPCs have equal probability (default)
        /// - HighCR: Higher CR NPCs are more likely to be selected (harder encounters)
        /// Defaults to <see cref="CRWeightPreference.Balanced"/>.
        /// </param>
        /// <param name="listPreference">
        /// Determines which NPC list to select from.
        /// - Random: 50/50 chance between humanoids and bestiary
        /// - Humanoids: Always select from humanoids (NPCs, friendly characters)
        /// - Bestiary: Always select from bestiary (monsters, beasts)
        /// Defaults to <see cref="CreatureListPreference.Random"/>.
        /// </param>
        /// <returns>
        /// A randomly selected NpcModel if successful, or null if:
        /// - NPC lists are empty or not loaded
        /// - An error occurs during selection
        /// </returns>
        /// <remarks>
        /// Selection Algorithm:
        /// 1. Load both humanoid and bestiary lists
        /// 2. Choose one list based on listPreference (Random = coin flip)
        /// 3. Apply CR weighting to selection within chosen list
        /// 4. Return selected NPC
        /// 5. If error occurs, log and return null
        /// 
        /// CR Weighting:
        /// - LowCR: NPCs with low CR get higher selection probability
        /// - Balanced: Linear probability distribution
        /// - HighCR: NPCs with high CR get higher selection probability
        /// 
        /// Example:
        /// var npc = NpcRandomizer(CRWeightPreference.HighCR, CreatureListPreference.Humanoids);
        /// // Result: Random high-CR humanoid (boss-like encounter)
        /// </remarks>
        public static NpcModel? NpcRandomizer(CRWeightPreference crMode = CRWeightPreference.Balanced, CreatureListPreference listPreference = CreatureListPreference.Random)
        {
            try
            {
                // Step 1: Ensure NPC lists are loaded and available
                var (humanoids, bestiary) = LoadLists();

                // Step 2: Choose which list to use based on preference
                var selectedList = ChooseListWeighted(humanoids, bestiary, listPreference);

                // Step 3: Select a random NPC from chosen list with CR weighting applied
                var pickedNpcWeighted = PickRandomNpcWeighted(selectedList, crMode);

                // Log the selected NPC for debugging
                LogService.Info($"[EncounterRandomizer.NpcRandomizer]\n\n> Picked NPC: {pickedNpcWeighted!.Name}\n\n");
                return pickedNpcWeighted;
            }
            catch (Exception ex)
            {
                // Log any errors and return null as safe fallback
                LogService.Error($"[EncounterService.NpcRandomizer] > Error:\n{ex.Message}");
                return null;
            }
        }

        #endregion NPC RANDOMIZER

        #region === HELPER METHODS & ENUMS ===

        /// <summary>Random instance for all NPC selection operations.</summary>
        private static readonly Random _random = new Random();

        /// <summary>
        /// Enumerates the different challenge rating weighting modes for NPC selection.
        /// 
        /// This determines how the Challenge Rating (CR) of NPCs influences their
        /// probability of being selected in an encounter.
        /// </summary>
        public enum CRWeightPreference
        {
            /// <summary>Lower CR NPCs are more likely to be selected (easier encounters).</summary>
            LowCR,

            /// <summary>All NPCs have equal selection probability regardless of CR (default).</summary>
            Balanced,

            /// <summary>Higher CR NPCs are more likely to be selected (harder encounters).</summary>
            HighCR
        }

        /// <summary>
        /// Enumerates the different creature list sources for NPC selection.
        /// 
        /// This determines which NPC collection (humanoids vs monsters) to draw encounters from.
        /// </summary>
        public enum CreatureListPreference
        {
            /// <summary>50/50 chance between humanoids and bestiary (default).</summary>
            Random,

            /// <summary>Always select from humanoids (NPCs, people-like creatures).</summary>
            Humanoids,

            /// <summary>Always select from bestiary (monsters, beasts, non-humanoid creatures).</summary>
            Bestiary
        }

        /// <summary>
        /// Ensures humanoids and bestiary lists are loaded and available.
        /// 
        /// Checks if NPC lists exist in GameData. If both are empty or null,
        /// attempts to reload them from the HumanoidLoader to ensure data is available.
        /// </summary>
        /// <returns>
        /// A tuple of (humanoids list, bestiary list). Neither will be null,
        /// though they may be empty if loading fails.
        /// </returns>
        /// <remarks>
        /// This method prevents failures due to uninitialized data by providing
        /// automatic fallback loading. It's defensive programming for robustness.
        /// </remarks>
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
        private static NpcModel? PickRandomNpcWeighted(List<NpcModel> list, CRWeightPreference mode)
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
                case CRWeightPreference.LowCR:
                    // Inverse weight: lower CR gets higher weight
                    weights = list.Select(npc => 1.0 / Math.Max(npc.CR, 0.01)).ToList();
                    break;

                case CRWeightPreference.Balanced:
                    // All NPCs have equal chance
                    weights = list.Select(_ => 1.0).ToList();
                    break;

                case CRWeightPreference.HighCR:
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
