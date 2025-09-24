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
        private static readonly Random _rnd = new();

        /// <summary>
        /// Generates a dynamic battle message for any attack.
        /// </summary>
        public static string GenerateAttackText(
            string attackerName,
            string defenderName,
            WeaponModel weapon,
            ProcessRollsAndDamage.HitResult hitResult,
            int damage,
            int critDamage,
            int totalDamage,
            int attackRoll,
            int abilityMod,
            int proficiencyMod,
            int currentDefenderHP,
            int maxDefenderHP,
            int cr,
            List<string>? statusEffects = null)
        {
            // Load text container once
            var container = TextLoader.LoadContainer();
            string message = string.Empty;

            // Determine text category
            Func<TextContainer, List<TextModel>?> selector = hitResult switch
            {
                ProcessRollsAndDamage.HitResult.IsCriticalHit => c => c.CriticalHit,
                ProcessRollsAndDamage.HitResult.IsCriticalMiss => c => c.CriticalMiss,
                ProcessRollsAndDamage.HitResult.IsValidHit => c => c.Hit,
                _ => c => c.Miss
            };

            // Pick a random template
            var template = container != null && selector(container) != null && selector(container)!.Count > 0
                ? selector(container)![_rnd.Next(selector(container)!.Count)].Text
                : "{attacker} attacks {defender} with {weapon}!";

            // Replace placeholders
            message = template
                .Replace("{attacker}", attackerName)
                .Replace("{defender}", defenderName)
                .Replace("{weapon}", weapon.Name)
                .Replace("{damage}", damage.ToString())
                .Replace("{critDamage}", critDamage.ToString())
                .Replace("{totalDamage}", totalDamage.ToString())
                .Replace("{attackRoll}", attackRoll.ToString())
                .Replace("{abilityMod}", abilityMod.ToString())
                .Replace("{proficiencyMod}", proficiencyMod.ToString())
                .Replace("{currentHP}", currentDefenderHP.ToString())
                .Replace("{maxHP}", maxDefenderHP.ToString())
                .Replace("{cr}", cr.ToString());

            // Add optional status effects
            if (statusEffects != null && statusEffects.Count > 0)
            {
                message += "\nStatus Effects: " + string.Join(", ", statusEffects);
            }

            // Optional: append outcome text based on HP
            if (currentDefenderHP <= 0)
            {
                message += $"\n💀 {defenderName} is defeated!";
            }

            return message;
        }
    }
}
