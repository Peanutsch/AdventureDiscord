using Adventure.Models.Text;
using System;

namespace Adventure.Quest.Battle.TextGenerator
{
    public static class BattleTextHelper
    {
        private static readonly Random rng = new Random();

        public static string GetRandomHpStatusText(string statusLabel, BattleTextModel battleText)
        {
            if (battleText.HpStatus.TryGetValue(statusLabel, out var entries) && entries.Count > 0)
            {
                int index = rng.Next(entries.Count);
                return entries[index].Text;
            }
            return "has an unknown status, because the Dev fvcked up...";
        }
    }
}
