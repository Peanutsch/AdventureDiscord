using Adventure.Loaders;
using Adventure.Models.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Adventure.Quest.Battle.Randomizers
{
    internal static class TextRandomizer
    {
        private static readonly Random _rnd = new();

        /// <summary>
        /// Selects a random TextModel from a category and replaces placeholders.
        /// Supports optional plural forms using {placeholder:plural}.
        /// </summary>
        public static string? GetRandomText(Func<TextContainer, List<TextModel>?> selector,
                                            Dictionary<string, string>? placeholders = null,
                                            Dictionary<string, string>? plurals = null)
        {
            var container = TextLoader.LoadContainer();
            if (container == null) return null;

            var list = selector(container);
            if (list == null || list.Count == 0) return null;

            var chosen = list[_rnd.Next(list.Count)]?.Text;
            if (chosen == null) return null;

            // Replace standard placeholders
            if (placeholders != null)
            {
                foreach (var kvp in placeholders)
                {
                    chosen = chosen.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            // Replace plural placeholders {placeholder:plural}
            if (plurals != null)
            {
                var regex = new Regex(@"\{(\w+):plural\}");
                chosen = regex.Replace(chosen, match =>
                {
                    var key = match.Groups[1].Value;
                    if (plurals.TryGetValue(key, out var pluralValue))
                        return pluralValue;
                    return match.Value; // fallback: laat staan
                });
            }

            return chosen;
        }
    }
}
