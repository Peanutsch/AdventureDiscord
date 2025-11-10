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
        #region === Move Player ===
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
            var components = ButtonBuildersMap.BuildDirectionButtons(targetTile);

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embed.Build();
                msg.Components = components.Build();
            });

            return true;
        }
        #endregion

        #region === Transition Embeds ===
        public static async Task TransferAnimationEmbed(SocketInteractionContext context, TileModel targetTile)
        {
            var areaName = TestHouseLoader.AreaLookup.TryGetValue(targetTile.AreaId, out var area)
                ? area.Name
                : targetTile.AreaId;

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("Travel 🏃")
                    .WithDescription($"Traveling to **{areaName}**...")
                    .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286889060175972/iu_.png?ex=6912b139&is=69115fb9&hm=5a328c96b633cf372af46cfb0cfd8a8ffddc22b602562905e69ca47c5c9d492d&")
                    .WithColor(Color.Orange)
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(2500);
        }

        public static async Task TransferBattleEmbed(SocketInteractionContext context, string npc)
        {
            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("⚔️ GET READY ⚔️")
                    .WithDescription($"Get ready to fight **{npc}**...")
                    .WithColor(Color.Red)
                    .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286545307598969/iu_.png?ex=6912b0e7&is=69115f67&hm=78332d8954422f6b3a261847abea4eba4d30ffa38e10fe9b92da4a03949940ef&")
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(2500);
        }

        public static async Task TransferFleeEmbed(SocketInteractionContext context)
        {
            await context.Interaction.DeferAsync();

            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("🏃 Flee 🏃")
                    .WithDescription($"You fled as fast as you can...")
                    .WithColor(Color.Orange)
                    .WithImageUrl("https://cdn.discordapp.com/attachments/1425057075314167839/1437286170718371900/iu_.png?ex=6912b08e&is=69115f0e&hm=bc2b7fd46742a8811318978c2f8d91567cc952aa923a5fdc332987fcf7f12315&")
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            await Task.Delay(2500);

            await ComponentInteractions.ReturnToWalkAsync(context);
        }
        #endregion
    }
}
