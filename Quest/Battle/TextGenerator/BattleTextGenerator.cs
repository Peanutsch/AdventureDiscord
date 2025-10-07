using Adventure.Data;
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
using System.Numerics;

namespace Adventure.Quest.Battle.TextGenerator
{
    public static class BattleTextGenerator
    {
        private static readonly Random rng = new Random();

        /// <summary>
        /// Generates a formatted battle log entry based on the attack result, attacker, defender, and battle state.
        /// </summary>
        /// <param name="attackResult">The result of the attack (e.g., hit, miss, criticalHit, criticalMiss).</param>
        /// <param name="attacker">The name of the attacking entity.</param>
        /// <param name="defender">The name of the defending entity.</param>
        /// <param name="weapon">The weapon used by the attacker.</param>
        /// <param name="damage">The amount of damage dealt (if applicable).</param>
        /// <param name="statusLabel">The status key (e.g., "healthy", "wounded").</param>
        /// <param name="battleText">Model containing text templates for different outcomes.</param>
        /// <param name="state">The current battle state with roll and player data.</param>
        /// <param name="rollText">Dictionary of roll text templates for formatting dice results.</param>
        /// <param name="strength">The attacker's strength value (for damage calculation).</param>
        /// <param name="isPlayerAttack">Whether the attack is performed by the player.</param>
        /// <returns>A formatted string describing the full battle log entry.</returns>
        public static string GenerateBattleLog(string attackResult, string attacker, string defender, string weapon, int damage, string statusLabel, BattleTextModel battleText,
                                       BattleState state, Dictionary<string, string>? rollText, int strength, bool isPlayerAttack = true)
        {
            string attackText = "";

            // Kies random tekst afhankelijk van het aanvalstype
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
                    attackText = $"Dev made a fvck up! {attacker} starts a striptease, which hurts the eyes of {defender}.";
                    break;
            }

            // Voeg roll details toe als speler aanvalt
            string? rollDetails = GetRollDetails(state, rollText, isPlayerAttack);
            if (!string.IsNullOrEmpty(rollDetails))
            {
                attackText += $"\n{rollDetails}";
            }

            // HP-status: random keuze uit beschikbare variaties
            string statusText = "has an unknown status, because the Dev fvcked up...";
            if (battleText.HpStatus.TryGetValue(statusLabel, out var statusEntries) && statusEntries is List<TextEntry> entries && entries.Count > 0)
            {
                int index = new Random().Next(entries.Count);
                statusText = entries[index].Text;
            }

            return $"{attackText}\n\n**{defender}** is **{statusLabel}** and {statusText}";
        }


        /// <summary>
        /// Selects a random text entry from a list and replaces placeholders with actual values.
        /// </summary>
        /// <param name="entries">A list of possible text entries.</param>
        /// <param name="attacker">The attacking entity's name.</param>
        /// <param name="defender">The defending entity's name.</param>
        /// <param name="weapon">The weapon used in the attack.</param>
        /// <param name="damage">The damage dealt in the attack.</param>
        /// <returns>A string with placeholders replaced, or an empty string if no entries exist.</returns>
        public static string GetRandomText(List<TextEntry> entries, string attacker, string defender, string weapon, int damage)
        {
            if (entries == null || entries.Count == 0)
                return "";

            // Pick a random template
            int index = rng.Next(entries.Count);
            string text = entries[index].Text;

            // Replace placeholders with actual values
            text = text.Replace("{attacker}", $"**{attacker}**")
                       .Replace("{defender}", $"**{defender}**")
                       .Replace("{weapon}", $"**{weapon}**")
                       .Replace("{damage}", $"{damage}");

            return text;
        }

        /// <summary>
        /// Generates a formatted string describing the result of a player's roll in battle.
        /// Includes attack rolls, damage rolls, and total damage depending on the outcome.
        /// String is returned with "> " to be used as blockquote in Discord Embed.
        /// </summary>
        /// <param name="state">The current battle state containing roll and damage info.</param>
        /// <param name="rollText">A dictionary of roll text templates loaded from JSON.</param>
        /// <param name="isPlayerAttack">Indicates whether the attack is from the player.</param>
        /// <returns>A formatted string describing the roll, or null if not applicable.</returns>
        public static string? GetRollDetails(BattleState state, Dictionary<string, string>? rollText, bool isPlayerAttack = true)
        {
            if (!isPlayerAttack || rollText == null)
                return null;

            // Critical hit: show only the critical damage roll
            if (state.IsCriticalHit)
            {
                string attackRoll = ReplacePlaceholders(rollText["attackRoll"], state);
                string damageRoll = ReplacePlaceholders(rollText["damageRoll"], state);
                string critDamageRoll = ReplacePlaceholders(rollText["critDamageRoll"], state);
                string totalDamageCriticalRoll = ReplacePlaceholders(rollText["totalDamageCriticalRoll"], state);

                return $"> {attackRoll}\n> {damageRoll}\n> {critDamageRoll}\n> {totalDamageCriticalRoll}";
            }
            // (Critical) Miss: show only the attack roll
            else if (state.IsCriticalMiss || state.HitResult == "isMiss")
            {
                string missRoll = ReplacePlaceholders(rollText["attackRoll"], state);
                return $"> {missRoll}";
            }
            else
            {
                // Normal hit: combine attack roll, damage roll, and total damage
                string attackRoll = ReplacePlaceholders(rollText["attackRoll"], state);
                string damageRoll = ReplacePlaceholders(rollText["damageRoll"], state);
                string total = ReplacePlaceholders(rollText["totalDamage"], state);

                return $"> {attackRoll}\n> {damageRoll}\n> {total}";
            }
        }

        /*
            Battle Roll Text Placeholders

            These placeholders can be used in roll templates (e.g., attackRoll, damageRoll, totalDamage) and will be replaced with actual values from the `BattleStateModel`.

            | Placeholder            | Description                                                                     |
            |-------------------------|--------------------------------------------------------------------------------|
            | `{TotalAttackRoll}`     | The result of the attack roll after adding modifiers (attack roll + modifiers). |
            | `{AttackRoll}`          | The raw dice roll result for the attack.                                       |
            | `{AbilityModifier}`     | The attacker's ability modifier (e.g., from Strength or Dexterity).            |
            | `{Strength}`            | The attacker's strength attribute value.                                       |
            | `{strength}`            | Alias for `{Strength}` (same value).                                           |
            | `{ProficiencyModifier}` | The attacker's proficiency bonus (based on level and proficiencies).           |
            | `{PlayerLevel}`         | The attacker's current level.                                                  |
            | `{Dice}`                | The dice result used for calculating damage (e.g., d6, d8).                    |
            | `{Damage}`              | The amount of damage dealt (before modifiers).                                 |
            | `{CritRoll}`            | The dice result used for critical hit damage (if applicable).                  |
            | `{TotalDamage}`         | The result of the damage roll after adding modifiers (damage roll + modifiers).|
         */


        /// <summary>
        /// Replaces all placeholders in a roll template string with actual values from a BattleStateModel.
        /// </summary>
        /// <param name="template">The template string containing placeholders (e.g., {TotalAttackRoll}, {Damage}).</param>
        /// <param name="state">The current battle state containing all relevant values.</param>
        /// <returns>A string with placeholders replaced by their corresponding values.</returns>
        public static string ReplacePlaceholders(string template, BattleState state)
        {
            if (template == null)
                return string.Empty;

            // Replace all placeholders with actual battle values
            return template
                .Replace("{TotalAttackRoll}", $"{state.TotalAttackRoll}")
                .Replace("{AttackRoll}", $"{state.AttackRoll}")
                .Replace("{Attackroll}", $"{state.AttackRoll}")
                .Replace("{AbilityModifier}", $"{state.AbilityModifier}")
                .Replace("{Strength}", $"{state.Player.Attributes.Strength}")
                .Replace("{strength}", $"{state.Player.Attributes.Strength}")
                .Replace("{ProficiencyModifier}", $"{state.ProficiencyModifier}")
                .Replace("{PlayerLevel}", $"{state.Player.Level}")
                .Replace("{Dice}", $"{state.Dice}")
                .Replace("{Damage}", $"{state.Damage}")
                .Replace("{damage}", $"{state.Damage}")
                .Replace("{CritRoll}", $"{state.CritRoll}")
                .Replace("{critRoll}", $"{state.CritRoll}")
                .Replace("{TotalDamage}", $"{state.TotalDamage}");
        }
    }
}
