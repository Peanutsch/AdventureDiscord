using Adventure.Loaders;
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

        public static string GenerateBattleLog(
            string attackType,       // "criticalHit", "hit", "criticalMiss", "miss"
            string attacker,
            string defender,
            string weapon,
            int damage,
            string statusLabel,      // bijv. "Bloodied"
            BattleTextModel battleText)   // je JSON object
        {
            string attackText = "";

            // 1️⃣ Kies een willekeurige tekst voor het aanvalstype
            switch (attackType) {
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

            // 2️⃣ Voeg HP-status flavour text toe
            string statusText = battleText.HpStatus.ContainsKey(statusLabel)
                ? battleText.HpStatus[statusLabel]
                : "has an unknown status.";

            return $"{attackText}\n🧟 {defender} is **{statusLabel}** and {statusText}";
        }

        private static string GetRandomText(
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
    }
}
