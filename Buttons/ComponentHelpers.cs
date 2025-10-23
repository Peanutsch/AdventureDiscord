using Adventure.Loaders;
using Adventure.Quest.Map;
using Adventure.Services;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Buttons
{
    public static class ComponentHelpers
    {
        /// <summary>
        /// Moves the player to a new tile and updates the embed and buttons.
        /// </summary>
        public static async Task<bool> MovePlayerAsync(SocketInteractionContext context, string key, bool showTravelAnimation = false)
        {
            // --- Try to retrieve the target tile ---
            if (!MainHouseLoader.TileLookup.TryGetValue(key, out var targetTile) || targetTile == null)
            {
                LogService.Error($"[MovementHelper] ❌ Target tile '{key}' not found.");
                await context.Interaction.FollowupAsync($"❌ Target tile '{key}' not found.", ephemeral: true);
                return false;
            }

            // --- Save the new position ---
            LogService.Info($"[MovementHelper] Saving new savepoint for {context.User.GlobalName}/{context.User.Id}, savepoint: {key}.");
            JsonDataManager.UpdatePlayerSavepoint(context.User.Id, key);

            // --- Optionally show a travel animation ---
            if (showTravelAnimation)
            {
                await TransferAnimationEmbed(context, key);
            }

            // --- Build the updated embed and buttons ---
            var embed = EmbedBuildersMap.EmbedWalk(targetTile);
            var components = EmbedBuildersMap.BuildDirectionButtons(targetTile)
                ?? new ComponentBuilder().WithButton("[Break]", "btn_flee", ButtonStyle.Secondary, row: 2);

            // --- Update the original message ---
            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embed.Build();
                msg.Components = components.Build();
            });

            return true;
        }

        /// <summary>
        /// Replace the original embed with a short "travel" animation.
        /// </summary>
        public static async Task TransferAnimationEmbed(SocketInteractionContext context, string targetTileId)
        {
            // Split targetTileId into "areaName" and "tile_{row}_{col}"
            var parts = targetTileId.Split(':');
            var roomName = parts[0];

            // --- Replace the existing message with a temporary "Moving..." embed ---
            await context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = new EmbedBuilder()
                    .WithTitle("🚶 Moving...")
                    .WithDescription($"Walking to **{roomName}**...")
                    .WithColor(Color.Orange)
                    .Build();

                msg.Components = new ComponentBuilder()
                    .WithButton("Please wait...", "none", ButtonStyle.Secondary, disabled: true)
                    .Build();
            });

            // --- Simulate travel time ---
            await Task.Delay(1500);
        }
    }
}
