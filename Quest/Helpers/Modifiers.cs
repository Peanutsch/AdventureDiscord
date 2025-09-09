using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Helpers
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
            if (levelOrCR >= 1 && levelOrCR <= 4) return 2; 

            else if (levelOrCR <= 8) return 3;

            else if (levelOrCR <= 12) return 4;

            else if (levelOrCR <= 16) return 5;

            else if (levelOrCR <= 20) return 6;

            else if (levelOrCR <= 24) return 7;

            else if (levelOrCR <= 28) return 8;

            else return 9;
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
