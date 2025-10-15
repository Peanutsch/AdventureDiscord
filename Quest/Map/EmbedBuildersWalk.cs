using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Map;
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
            LogService.DividerParts(1, "BuildDirectionButtons");

            //if (tile.TileExits != null)
            if (tile.TileGrid != null)
            {
                var builder = new ComponentBuilder();
                var exits = MapService.GetExits(tile, MapLoader.TileLookup);

                string[] directionOrder = { "West", "North", "South", "East" };

                foreach (var dir in directionOrder)
                {
                    if (exits.TryGetValue(dir, out var destination) && !string.IsNullOrEmpty(destination))
                    {
                        //string buttonText = $"{Label(dir)} to {destination}";
                        builder.WithButton(Label(dir), $"move_{dir.ToLower()}:{destination}", ButtonStyle.Primary);
                    }
                }

                builder.WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);

                LogService.Info("Returning builder.WithButton(\"[Break]\", \"btn_flee\", ButtonStyle.Secondary, row: 2);");
                LogService.DividerParts(2, "BuildDirectionButtons");
                return builder;
            }

            LogService.Error("[EmbedBuildersWalk.BuildDirectionButtons] tile.TileGrid = null...");
            return null;
        }
        #endregion

        #region === Embed Builders ===
        public static EmbedBuilder EmbedWalk(TileModel tile)
        {
            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Building embed...");

            var exits = MapService.GetExits(tile, MapLoader.TileLookup);
            var exitInfo = new StringBuilder();

            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Get exits...");
            foreach (var (exit, destination) in exits!)
            {
                LogService.Info($"{exit} leads to {destination}");
                exitInfo.AppendLine($"**{exit}** leads to **{destination}**");
            }

            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Get gridVisual...");
            
            var gridVisual = TileUI.RenderTileGrid(tile.TileGrid);
            
            LogService.Info($"Grid rendered:\n" +
                            $"{gridVisual}");

            var embed = new EmbedBuilder()
                //.WithTitle($"[Position: {tile.TileDescription}]")
                .WithColor(Color.Blue)
                .AddField($"[You are on tile *{tile.TileName}*]", $"{gridVisual}\n{tile.TileText}")
                .AddField($"[Possible Directions]", $"{exitInfo}");

            LogService.Info("[EmbedBuildersWalk.EmbedWalk] Returning embed...");
            return embed;
        }
        #endregion
    }
}
