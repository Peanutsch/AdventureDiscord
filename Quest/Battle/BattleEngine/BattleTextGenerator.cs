using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.Text;
using Adventure.Quest.Battle.Process;
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

        public static string? GetRollDetails(BattleStateModel state, Dictionary<string, string>? rollText, bool isPlayerAttack = true) {
            if (!isPlayerAttack || rollText == null)
                return null;

            // Helper functie voor placeholders
            static string ReplacePlaceholders(string template, BattleStateModel s) {
                return template
                    .Replace("{TotalAttackRoll}", s.TotalAttackRoll.ToString())
                    .Replace("{AttackRoll}", s.AttackRoll.ToString())
                    .Replace("{AbilityModifier}", s.AbilityModifier.ToString())
                    .Replace("{Strength}", s.Player.Attributes.Strength.ToString())
                    .Replace("{ProficiencyModifier}", s.ProficiencyModifier.ToString())
                    .Replace("{PlayerLevel}", s.Player.Level.ToString())
                    .Replace("{Dice}", s.Dice.ToString())
                    .Replace("{Damage}", s.Damage.ToString())
                    .Replace("{CritRoll}", s.CritRoll.ToString())
                    .Replace("{TotalDamage}", s.TotalDamage.ToString());
            }

            string attackRollText = "";
            string damageRollText = "";
            string criticalRollText = "";
            string totalDamageText = ReplacePlaceholders(rollText["totalDamage"], state); // altijd tonen

            if (state.IsCriticalHit) {
                attackRollText = ReplacePlaceholders(rollText["attackRoll"], state);
                damageRollText = ReplacePlaceholders(rollText["damageRoll"], state);
                criticalRollText = ReplacePlaceholders(rollText["criticalDamageRoll"], state);
            }
            else if (state.IsCriticalMiss) {
                attackRollText = ReplacePlaceholders(rollText["attackRoll"], state);
                damageRollText = "💀 Critical miss! No damage.";
                criticalRollText = "";
            }
            else {
                attackRollText = ReplacePlaceholders(rollText["attackRoll"], state);
                damageRollText = ReplacePlaceholders(rollText["damageRoll"], state);
                criticalRollText = "";
            }

            return $"{attackRollText}\n{damageRollText}\n{criticalRollText}\n{totalDamageText}".Trim();
        }
    }
}
