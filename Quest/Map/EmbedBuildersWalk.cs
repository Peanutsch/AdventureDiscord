using Adventure.Models.BattleState;
using Adventure.Models.Map;
using Adventure.Quest.Walk;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Map
{
    public class EmbedBuildersWalk
    {
        #region === Buttons ===
        public static string Label(string direction)
        {
            string label = direction switch
            {
                "West" => "⬅️ West",
                "North" => "⬆️ North",
                "South" => "⬇️ South",
                "East" => "➡️ East",
                _ => direction
            };

            return label;
        }

        public static ComponentBuilder? BuildDirectionButtons(TileModel tile)
        {
            if (tile.TileExits != null)
            {
                var builder = new ComponentBuilder();
                var exits = MapService.GetExits(tile);

                string[] directionOrder = { "West", "North", "South", "East" };

                foreach (var dir in directionOrder)
                {
                    if (exits.TryGetValue(dir, out var destination) && !string.IsNullOrEmpty(destination))
                    {
                        string buttonText = $"{Label(dir)} to {destination}";
                        builder.WithButton(buttonText, $"move_{dir.ToLower()}:{destination}", ButtonStyle.Primary);
                    }
                }

                builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);
                return builder;
            }

            return null;
        }
        #endregion

        #region === Embed Builders ===
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            var exits = MapService.GetExits(tile);
            var exitInfo = new StringBuilder();

            foreach (var (direction, destination) in exits)
            {
                exitInfo.AppendLine($"**{direction}** leads to **{destination}**");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"[Position: {tile.TileDescription}]")
                .WithColor(Color.Blue)
                .AddField(
                          $"[You are on tile *{tile.TileName}*]", 
                          $"{tile.TilePosition}\n" +
                          $"{tile.TileText}")
                .AddField($"[Possible Directions]", $"{exitInfo}");

            return embed;
        }
        #endregion
    }
}
