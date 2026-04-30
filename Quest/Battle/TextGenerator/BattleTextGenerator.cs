using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.Text;
using Adventure.Quest.Battle.Process;
using Adventure.Quest.Rolls;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Adventure.Quest.Battle.TextGenerator
{
    /// <summary>
    /// Generates formatted text for battle logs and combat rolls, 
    /// using text templates and battle state data.
    /// </summary>
    public static class BattleTextGenerator
    {
        // Global random instance for text variation
        private static readonly Random rng = new Random();

        #region === Generate Battle Log === 
        /// <summary>
        /// Generates a formatted battle log entry based on attack result, participants, and current battle state.
        /// </summary>
        /// <param name="attackResult">The type of attack outcome (e.g., hit, miss, criticalHit).</param>
        /// <param name="attacker">Name of the attacking entity.</param>
        /// <param name="defender">Name of the defending entity.</param>
        /// <param name="weapon">Weapon used in the attack.</param>
        /// <param name="damage">Amount of damage dealt (if applicable).</param>
        /// <param name="statusLabel">Health status label (e.g., wounded, dying).</param>
        /// <param name="battleText">Text template model containing descriptions for battle outcomes.</param>
        /// <param name="state">The current battle state model containing roll and character data.</param>
        /// <param name="rollText">Dictionary of text templates for roll details.</param>
        /// <param name="strength">Attacker’s strength attribute.</param>
        /// <param name="isPlayerAttack">True if the attacker is the player, false if NPC.</param>
        /// <returns>Formatted string describing the entire battle log entry.</returns>
        public static string GenerateBattleLog(
                            string attackResult,
                            string attacker,
                            string defender,
                            string weapon,
                            int damage,
                            string statusLabel,
                            BattleTextModel battleText,
                            BattleSession session,
                            Dictionary<string, string>? rollText,
                            bool isPlayerAttack = true
                                               )
        {

            // Pick random text template based on the attack result
            string attackText = attackResult switch
            {
                "criticalHit" => GetRandomText(battleText.CriticalHit, attacker, defender, weapon, damage),
                "hit" => GetRandomText(battleText.Hit, attacker, defender, weapon, damage),
                "criticalMiss" => GetRandomText(battleText.CriticalMiss, attacker, defender, weapon, damage),
                "miss" => GetRandomText(battleText.Miss, attacker, defender, weapon, damage),
                // Fallback message if an unknown attackResult is passed
                _ => $"The dev made a fvck up! {attacker} starts a striptease, which hurts the eyes of {defender} so much, {defender} commit's instantly hara-kiri.",
            };

            // Add dice roll details if this is a player attack
            string? rollDetails = GetRollDetails(session, rollText, isPlayerAttack);
            if (!string.IsNullOrEmpty(rollDetails))
                attackText += $"\n{rollDetails}";

            // Determine random HP status text for the defender
            string statusText = "has an unknown status, because the dev fvcked up...";
            if (battleText.HpStatus.TryGetValue(statusLabel, out List<TextEntry>? statusEntries) &&
                statusEntries is List<TextEntry> entries && entries.Count > 0)
            {
                int index = new Random().Next(entries.Count);
                statusText = entries[index].Text;
            }

            // Combine attack + roll details + status into one log message
            return $"{attackText}\n\n**{defender}** is **{statusLabel}** and {statusText}";
        }
        #endregion Generate Battle Log

        #region === Get Text === 
        /// <summary>
        /// Selects a random text template and replaces placeholders with actual values.
        /// </summary>
        /// <param name="entries">List of possible text templates for the action.</param>
        /// <param name="attacker">Name of the attacking entity.</param>
        /// <param name="defender">Name of the defending entity.</param>
        /// <param name="weapon">Weapon used in the attack.</param>
        /// <param name="damage">Damage dealt in this attack.</param>
        /// <returns>Formatted string with placeholder substitutions, or an empty string if none exist.</returns>
        public static string GetRandomText(List<TextEntry> entries, string attacker, string defender, string weapon, int damage)
        {
            if (entries == null || entries.Count == 0)
                return "has an unknown status, because the dev fvcked up...";

            // Randomly select a text template
            int index = rng.Next(entries.Count);
            string text = entries[index].Text;

            // Replace placeholders with dynamic values
            text = text.Replace("{attacker}", $"**{attacker}**")
                       .Replace("{defender}", $"**{defender}**")
                       .Replace("{weapon}", $"**{weapon}**")
                       .Replace("{damage}", $"{damage}");

            return text;
        }
        #endregion Get Text

        #region === Get Roll Details === 
        /// <summary>
        /// Builds a detailed breakdown of dice rolls and damage for the player’s attack.
        /// Used for display in Discord as blockquote lines (prefixed with '>').
        /// </summary>
        /// <param name="state">The current battle state model.</param>
        /// <param name="rollText">Dictionary containing text templates for rolls (loaded from JSON).</param>
        /// <param name="isPlayerAttack">Whether the current attack is from the player.</param>
        /// <returns>A formatted multiline string with roll details, or null if not applicable.</returns>
        public static string? GetRollDetails(BattleSession session, Dictionary<string, string>? rollText, bool isPlayerAttack = true)
        {
            if (!isPlayerAttack || rollText == null)
                return null;

            // Handle critical hit: show extra damage rolls
            if (session.State.IsCriticalHit)
            {
                string attackRoll = ReplacePlaceholders(rollText["attackRoll"], session);
                string damageRoll = ReplacePlaceholders(rollText["damageRoll"], session);
                string critDamageRoll = ReplacePlaceholders(rollText["critDamageRoll"], session);
                string totalDamageCriticalRoll = ReplacePlaceholders(rollText["totalDamageCriticalRoll"], session);

                return $"> {attackRoll}\n> {damageRoll}\n> {critDamageRoll}\n> {totalDamageCriticalRoll}";
            }
            // Handle miss and critical miss: show only attack roll
            else if (session.State.IsCriticalMiss || session.State.HitResult == "isMiss")
            {
                string missRoll = ReplacePlaceholders(rollText["attackRoll"], session);
                return $"> {missRoll}";
            }
            else
            {
                // Normal hit: show attack roll, damage roll, and total damage
                string attackRoll = ReplacePlaceholders(rollText["attackRoll"], session);
                string damageRoll = ReplacePlaceholders(rollText["damageRoll"], session);
                string total = ReplacePlaceholders(rollText["totalDamage"], session);

                return $"> {attackRoll}\n> {damageRoll}\n> {total}";
            }
        }
        #endregion Get Roll Details

        #region === Replace Placeholders === 
        /*
            Battle Roll Text Placeholders

            These placeholders are dynamically replaced with values from `BattleSession.State`.
            They allow flexible text templates for dice roll results.

            | Placeholder            | Description                                                                     |
            |------------------------|---------------------------------------------------------------------------------|
            | {TotalAttackRoll}      | Final attack roll (base roll + modifiers).                                      |
            | {AttackRoll}           | Raw dice result for the attack roll.                                            |
            | {AbilityModifier}      | The attacker's ability modifier (e.g., Strength or Dexterity).                  |
            | {Strength} / {strength}| The attacker's base Strength attribute.                                         |
            | {ProficiencyModifier}  | The attacker's proficiency bonus.                                               |
            | {PlayerLevel}          | The attacker's current level.                                                   |
            | {Dice}                 | The damage dice used (e.g., d6, d8).                                            |
            | {Damage} / {damage}    | Raw damage before modifiers.                                                    |
            | {CritRoll} / {critRoll}| Critical damage dice roll result.                                               |
            | {TotalDamage}          | Final total damage (including modifiers).                                       |
         */

        /// <summary>
        /// Replaces placeholder variables in a roll text template with real values from the current battle session.
        /// </summary>
        /// <param name="template">Template string containing placeholders (e.g., {Damage}, {AttackRoll}).</param>
        /// <param name="session">The current battle session with all relevant roll values.</param>
        /// <returns>String with all placeholders replaced by actual values.</returns>
        public static string ReplacePlaceholders(string template, BattleSession session)
        {
            if (template == null)
                return string.Empty;

            // Replace placeholders one by one using battle state data
            return template
                .Replace("{TotalAttackRoll}", $"{session.State.TotalAttackRoll}")
                .Replace("{AttackRoll}", $"{session.State.AttackRoll}")
                .Replace("{Attackroll}", $"{session.State.AttackRoll}")
                .Replace("{AbilityModifier}", $"{session.State.AbilityModifier}")
                .Replace("{Strength}", $"{session.Context.Player.Attributes.Strength}")
                .Replace("{strength}", $"{session.Context.Player.Attributes.Strength}")
                .Replace("{ProficiencyModifier}", $"{session.State.ProficiencyModifier}")
                .Replace("{PlayerLevel}", $"{session.Context.Player.Level}")
                .Replace("{Dice}", $"{session.State.Dice}")
                .Replace("{Damage}", $"{session.State.Damage}")
                .Replace("{damage}", $"{session.State.Damage}")
                .Replace("{CritRoll}", $"{session.State.CritRoll}")
                .Replace("{critRoll}", $"{session.State.CritRoll}")
                .Replace("{TotalDamage}", $"{session.State.TotalDamage}");
        }
        #endregion Replace Placeholders
    }
}
