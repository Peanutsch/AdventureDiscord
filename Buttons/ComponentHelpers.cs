using Adventure.Loaders;
using Adventure.Models.Map;
using Adventure.Quest.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace Adventure.Buttons
{
    public static class ComponentHelpers
    {
        public static async Task<bool> MovePlayerAsync(SocketInteractionContext context, string key, bool showTravelAnimation = false)
        {
            if (!TestHouseLoader.TileLookup.TryGetValue(key, out var targetTile) || targetTile == null)
            {
                LogService.Error($"[ComponentHelpers.MovePlayerAsync] ❌ Target tile '{key}' not found!");
                await context.Interaction.FollowupAsync($"❌ Target tile '{key}' not found.", ephemeral: true);
                return false;
            }

            // Save player position
            LogService.Info($"[ComponentHelpers.MovePlayerAsync] Save location {key} for {context.User.GlobalName}");
            JsonDataManager.UpdatePlayerSavepoint(context.User.Id, key);

            // Travel animation
            if (showTravelAnimation)
                await TransferAnimationEmbed(context, targetTile);

            // Update embed + buttons
            var embed = EmbedBuildersMap.EmbedWalk(targetTile);
            var components = EmbedBuildersMap.BuildDirectionButtons(targetTile);

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embed.Build();
                msg.Components = components.Build();
            });

            return true;
        }

        public static async Task TransferAnimationEmbed(SocketInteractionContext context, TileModel targetTile)
        {
            var areaName = TestHouseLoader.AreaLookup.TryGetValue(targetTile.AreaId, out var area)
                ? area.Name
                : targetTile.AreaId;

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("🏃 Moving...")
                    .WithDescription($"Moving to **{areaName}**...")
                    .WithColor(Color.Orange)
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(1500);
        }
    }
}
