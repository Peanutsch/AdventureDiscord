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
                "North" => "⬆️ North",
                "East" => "➡️ East",
                "South" => "⬇️ South",
                "West" => "⬅️ West",
                _ => direction
            };

            return label;
        }

        public static ComponentBuilder? BuildDirectionButtons(TileModel map)
        {
            if (map.TileExits != null)
            {
                var builder = new ComponentBuilder();
                var connections = MapService.GetExits(map);

                foreach (var connection in connections)
                {
                    var buttonTex = $"{Label(connection.Key)} to {connection.Value}";

                    builder.WithButton(buttonTex, $"move_{connection.Key.ToLower()}:{connection.Value}", ButtonStyle.Primary);
                }
                    

                builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary);
                return builder;
            }

            return null;
        }
        #endregion

        #region === Embed Builders ===
        public static EmbedBuilder EmbedWalk(TileModel map)
        {
            var exits = MapService.GetExits(map);
            var exitInfo = new StringBuilder();

            foreach (var (direction, destination) in exits)
            {
                exitInfo.AppendLine($"**{direction}** leads to **{destination}**");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"[Position: {map.TileDescription}]")
                .WithColor(Color.Blue)
                .AddField(
                          $"[You are on tile *{map.TileName}*]", 
                          $"There is nothing to do here...")
                .AddField($"[Possible Directions]", $"{exitInfo}");

            return embed;
        }
        #endregion
    }
}
