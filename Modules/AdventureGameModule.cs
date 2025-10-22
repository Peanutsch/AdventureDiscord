using Adventure.Models.Map;
using Adventure.Services;
using Adventure.Models.Player;
using Discord.Interactions;
using System.Collections.Concurrent;
using Discord;
using Adventure.Loaders;
using Adventure.Data;
using Microsoft.VisualBasic;
using Adventure.Quest.Encounter;
using Adventure.Models.BattleState;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Map;

namespace Adventure.Modules
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        #region === Slashcommand "start" ===
        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task SlashCommandStartHandler()
        {
            var user = Context.Client.GetUser(Context.User.Id); 
            if (user != null)
            {
                string displayName = user.GlobalName ?? user.Username;
                LogService.Info($"[Encounter] Discord user '{displayName}' (ID: {user.Id})");
            }

            // Defer the response to prevent the "No response" error
            await DeferAsync();

            LogService.SessionDivider('=', "START");
            LogService.Info("[AdventureGameModule.SlashCommandStartHandler] > Slash Command /start is executed");

            // Reset inventory to basic inventory: Shordsword and Dagger
            //InventoryStateService.LoadInventory(Context.User.Id);

            // Send a follow-up response after the processing is complete.
            //await FollowupAsync("Slash Command /start is executed");
            await FollowupAsync("Your adventure has begun!");
        }
        #endregion

        #region === Slashcommand "encounter" ===
        // Trigger encounter for testing
        [SlashCommand("encounter", "Triggers a random encounter")]
        public async Task SlashCommandEncounterHandler()
        {
            await DeferAsync();

            var user = SlashCommandHelpers.GetDiscordUser(Context ,Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: Encounter");
            LogService.Info($"[/Encounter] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            var player = SlashCommandHelpers.GetOrCreatePlayer(user.Id, user.GlobalName ?? user.Username);
            if (player == null)
            {
                await FollowupAsync("⚠️ Internal error while creating or loading player.");
                return;
            }

            var npc = EncounterRandomizer.NpcRandomizer();

            if (npc == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                return;
            }

            SlashCommandHelpers.SetupBattleState(user.Id, npc);

            var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            var buttons = SlashCommandHelpers.BuildEncounterButtons();

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
        #endregion

        #region === Slashcommand "walk" ===
        [SlashCommand("walk", "Simulate walking through tiles with directional buttons.")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();

            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "SlashCommand: walk");
            LogService.Info($"[/walk] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            var player = JsonDataManager.LoadPlayerFromJson(user.Id);
            var tile = SlashCommandHelpers.GetTileFromSavePoint(player!.Savepoint);

            if (tile == null)
            {
                await FollowupAsync("❌ No valid savepoint or starting tile found.", ephemeral: true);
                return;
            }

            string startingRoom = MainHouseLoader.Rooms
                .FirstOrDefault(r => r.Value.Contains(tile)).Key ?? "UnknownRoom";

            LogService.Info($"[/walk] Starting in room: {startingRoom}, position: {tile.TilePosition}");

            var embed = EmbedBuildersWalk.EmbedWalk(tile);
            var components = EmbedBuildersWalk.BuildDirectionButtons(tile);

            await FollowupAsync(embed: embed.Build(), components: components?.Build());
        }


        /*
        /// <summary>
        /// Simulates walking through the map.
        /// </summary>
        [SlashCommand("walk", "Simulate walk over tiles with direction buttons...")]
        public async Task SlashCommandWalkHandler()
        {
            await DeferAsync();

            var user = SlashEncounterHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: walk");
            LogService.Info($"[/walk] Triggered by {user.GlobalName ?? user.Username} (userId: {user.Id})");

            // ✅ Zoek begin tile
            var startTile = MainHouseLoader.AllTiles
                    .FirstOrDefault(t => t.TileGrid?
                    .Any(row => row
                    .Any(cell => cell
                    .Equals("START", StringComparison.OrdinalIgnoreCase))) == true);

            if (startTile == null)
            {
                LogService.Error("[SlashCommandWalkHandler] Could not find START tile in MainHouseLoader.AllTiles.");
                await FollowupAsync("❌ No starting tile found (no tile contains 'START').", ephemeral: true);
                return;
            }

            // ✅ Probeer de kamer te bepalen via de Rooms dictionary
            string? startingRoom = MainHouseLoader.Rooms
                .FirstOrDefault(r => r.Value.Contains(startTile)).Key;

            if (string.IsNullOrEmpty(startingRoom))
            {
                LogService.Error("[SlashCommandWalkHandler] START tile found but could not determine room. Defaulting to 'UnknownRoom'.");
                startingRoom = "UnknownRoom";
            }

            // ✅ Maak de unieke key
            LogService.Info($"[/walk] Found START tile: {startingRoom}, {startTile.TilePosition}");

            string startingKey = $"{startingRoom}:{startTile.TilePosition}";            

            // ✅ Controleer of de key in TileLookup bestaat
            if (!MainHouseLoader.TileLookup.TryGetValue(startingKey, out var tile))
            {
                LogService.Error($"[SlashCommandWalkHandler] Tile '{startingKey}' not found in TileLookup.");
                await FollowupAsync($"❌ Tile '{startingKey}' not found.", ephemeral: true);
                return;
            }

            // ✅ Bouw embed en navigatieknoppen
            var embed = EmbedBuildersWalk.EmbedWalk(tile);
            var components = EmbedBuildersWalk.BuildDirectionButtons(tile);

            LogService.Info("[SlashCommandWalkHandler] Sending embed + components...");
            await FollowupAsync(embed: embed.Build(), components: components?.Build());
        }
        */
        #endregion
    }
}