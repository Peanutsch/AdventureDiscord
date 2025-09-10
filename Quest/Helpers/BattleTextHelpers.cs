using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Helpers
{
    internal class BattleTextHelpers
    {
        private static readonly Random rng = new();

        public static string GetRandomCriticalHitText(string attacker, string defender, string weapon, int totalDamage)
        {
            var options = new List<string>
        {
            $"🗡️ **[CRITICAL HIT] {attacker} strikes {defender} with {weapon}, causing a devastating wound for `{totalDamage}` damage!**",
            $"💥 **[CRITICAL HIT] {attacker}'s {weapon} tears into {defender}, dealing `{totalDamage}` damage in a brutal strike!**",
            $"⚔️ **[CRITICAL HIT] {attacker} delivers a deadly blow to {defender} with {weapon}, inflicting `{totalDamage}` damage!**"
        };

            return options[rng.Next(options.Count)];
        }

        public static string GetRandomMissText(string attacker, string defender, string weapon)
        {
            var options = new List<string>
        {
            $"🗡️ **[MISS] {attacker} swings their {weapon}, but misses {defender}!**",
            $"😬 **[MISS] {attacker} tries to hit {defender} with {weapon}, but fails miserably.**",
            $"🛡️ **[MISS] {defender} deflects the attack from {attacker}'s {weapon}!**"
        };

            return options[rng.Next(options.Count)];
        }
    }
}
