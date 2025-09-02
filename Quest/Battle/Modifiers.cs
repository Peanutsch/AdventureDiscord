using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle
{
    class Modifiers
    {
        #region MODIFIERS
        /// <summary>
        /// Calculates the proficiency bonus for a player or creature based on their 
        /// level (for players) or Challenge Rating (CR) (for creatures) according to D&D 5e rules.
        /// </summary>
        /// <param name="levelOrCR">
        /// The level of the player or the Challenge Rating of the creature.
        /// </param>
        /// <returns>
        /// The proficiency bonus as an integer.
        /// </returns>
        public static int GetProficiencyModifier(int levelOrCR)
        {
            if (levelOrCR >= 1 && levelOrCR <= 4)
                return 2; // Level 1–4 or CR 1–4 → ability/proficiency +2

            else if (levelOrCR <= 8)
                return 3;              // Level 5–8 → ability/proficiency +3

            else if (levelOrCR <= 12)
                return 4;              // Level 9–12 → ability/proficiency +4

            else if (levelOrCR <= 16)
                return 5;             // Level 13–16 → ability/proficiency +5

            else if (levelOrCR <= 20)
                return 6;             // Level 17–20 → ability/proficiency +6

            else if (levelOrCR <= 24)
                return 7;             // Level 21–24 → ability/proficiency +7

            else if (levelOrCR <= 28)
                return 8;             // Level 25–28 → ability/proficiency +8

            else
                return 9;                                  // Level 29–30 → ability/proficiency +9
        }

        public static int GetAbilityModifier(int abilityScore)
        {
            var result = (int)Math.Floor((abilityScore - 10) / 2.0);

            LogService.Info($"[Modifiers.GetAbilityModifier] Ability Score: {abilityScore} Modifier: {result}");

            return result;
        }
        #endregion MODIFIERS
    }
}
