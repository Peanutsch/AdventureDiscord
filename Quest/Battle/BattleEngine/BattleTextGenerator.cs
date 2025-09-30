using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.Text;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Helpers;
using Adventure.Quest.Rolls;
using Adventure.Services;
using System;
using System.Collections.Generic;

namespace Adventure.Quest.Battle.BattleEngine
{
    public static class BattleTextGenerator
    {
        private static readonly Random rng = new Random();

        public static string GenerateBattleLog(string attackResult, string attacker, string defender, string weapon,int damage,string statusLabel,
                                               BattleTextModel battleText, BattleStateModel state, Dictionary<string, string>? rollText,int strength,                            
                                               bool isPlayerAttack = true) 
        {
            string attackText = "";

            // 1️⃣ Kies een willekeurige flavour text
            switch (attackResult) 
            {
                case "criticalHit":
                    attackText = GetRandomText(battleText.CriticalHit, attacker, defender, weapon, damage);
                    break;
                case "hit":
                    attackText = GetRandomText(battleText.Hit, attacker, defender, weapon, damage);
                    break;
                case "criticalMiss":
                    attackText = GetRandomText(battleText.CriticalMiss, attacker, defender, weapon, damage);
                    break;
                case "miss":
                    attackText = GetRandomText(battleText.Miss, attacker, defender, weapon, damage);
                    break;
                default:
                    attackText = $"{attacker} does something unknown to {defender}.";
                    break;
            }

            // 2️⃣ Voeg roll details toe als speler aanvalt
            string? rollDetails = GetRollDetails(state, rollText, isPlayerAttack);
            if (!string.IsNullOrEmpty(rollDetails)) {
                attackText += $"\n{rollDetails}";
            }

            // 3️⃣ Voeg HP-status flavour text toe
            string statusText = battleText.HpStatus.ContainsKey(statusLabel)
                ? battleText.HpStatus[statusLabel]
                : "has an unknown status.";

            return $"{attackText}\n🧟 {defender} is **{statusLabel}** and {statusText}";
        }


        public static string GetRandomText(
            List<TextEntry> entries,
            string attacker,
            string defender,
            string weapon,
            int damage) {
            if (entries == null || entries.Count == 0)
                return "";

            int index = rng.Next(entries.Count);
            string text = entries[index].Text;

            // Plaatsvervanging
            text = text.Replace("{attacker}", attacker)
                       .Replace("{defender}", defender)
                       .Replace("{weapon}", weapon)
                       .Replace("{damage}", damage.ToString());

            return text;
        }

        /// <summary>
        /// Generates a formatted string describing the result of a player's roll in battle.
        /// This includes attack rolls, damage rolls, and total damage, depending on whether
        /// the roll was a hit, critical hit, or critical miss.
        /// </summary>
        /// <param name="state">The current battle state containing roll and damage info.</param>
        /// <param name="rollText">A dictionary of roll text templates loaded from JSON.</param>
        /// <param name="strength">The player's strength, used in damage calculations.</param>
        /// <param name="isPlayerAttack">Indicates whether the attack is from the player.</param>
        /// <returns>A formatted string describing the roll, or null if not applicable.</returns>
        public static string? GetRollDetails(BattleStateModel state, Dictionary<string, string>? rollText, bool isPlayerAttack = true) 
        {
            if (!isPlayerAttack || rollText == null)
                return null;

            // If the attack was a critical hit, show only critical damage roll
            if (state.IsCriticalHit) {
                return ReplacePlaceholders(rollText["criticalDamageRoll"], state);
            }
            // If the attack was a critical miss, show only the attack roll
            else if (state.IsCriticalMiss) {
                return ReplacePlaceholders(rollText["attackRoll"], state);
            }
            else {
                // Normal hit: combine attack roll, damage roll, and total damage
                string attack = ReplacePlaceholders(rollText["attackRoll"], state);
                string damage = ReplacePlaceholders(rollText["damageRoll"], state);
                string total = ReplacePlaceholders(rollText["totalDamage"], state);

                return $"{attack}\n{damage}\n{total}";
            }
        }

        /// <summary>
        /// Replaces all placeholders in a roll template string with actual values from a BattleStateModel.
        /// </summary>
        /// <param name="template">The template string containing placeholders like {TotalAttackRoll}, {Damage}, etc.</param>
        /// <param name="state">The current battle state containing all relevant values.</param>
        /// <returns>A string with all placeholders replaced by their corresponding values.</returns>
        public static string ReplacePlaceholders(string template, BattleStateModel state) {
            if (template == null)
                return string.Empty;

            // Replace all template placeholders with actual battle values
            return template
                .Replace("{TotalAttackRoll}", state.TotalAttackRoll.ToString())
                .Replace("{AttackRoll}", state.AttackRoll.ToString())
                .Replace("{AbilityModifier}", state.AbilityModifier.ToString())
                .Replace("{Strength}", state.Player.Attributes.Strength.ToString())
                .Replace("{ProficiencyModifier}", state.ProficiencyModifier.ToString())
                .Replace("{PlayerLevel}", state.Player.Level.ToString())
                .Replace("{Dice}", state.Dice.ToString())
                .Replace("{Damage}", state.Damage.ToString())
                .Replace("{CritRoll}", state.CritRoll.ToString())
                .Replace("{strength}", state.Player.Attributes.Strength.ToString());
        }

    }
}
