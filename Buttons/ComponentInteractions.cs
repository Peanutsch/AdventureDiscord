using Adventure.Loaders;
using Adventure.Modules;
using Adventure.Quest.Battle.BattleEngine;
using Adventure.Quest.Battle.Randomizers;
using Adventure.Quest.Encounter;
using Adventure.Services;
using Discord;
using Discord.Interactions;

namespace Adventure.Buttons
{
    public class ComponentInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        #region === Component Dispatch ===
        [ComponentInteraction("*")]
        public async Task DispatchComponentAction(string id)
        {
            LogService.Info($"[ComponentInteractions.DispatchComponentAction] component ID: {id}");

            if (id.StartsWith("weapon_"))
                await HandleWeaponButton($"{id}");
            else if (id.StartsWith("move:"))
                await WalkDirectionHandler(id);
            else if (id.StartsWith("enter:"))
                await EnterTileHandler(id);
            else if (id.StartsWith("encounter:"))
                await EncounterHandler();
            else if (id.StartsWith("battle_continue"))
                await ContinueBattleHandler(id);
            else if (id.StartsWith("battle_flee"))
                await FleeBattleHandler(id);
        }
        #endregion

        #region === Battle ===
        public async Task EncounterHandler()
        {
            await DeferAsync();

            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            if (user == null)
            {
                await FollowupAsync("⚠️ Error loading user data.");
                return;
            }

            var npc = EncounterRandomizer.NpcRandomizer();
            if (npc == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                return;
            }

            // Start battle
            await ComponentHelpers.TransitionBattleEmbed(Context, npc.Name!);

            SlashCommandHelpers.SetupBattleState(user.Id, npc);

            var embed = EmbedBuildersEncounter.EmbedRandomEncounter(npc);
            var buttons = SlashCommandHelpers.BuildEncounterButtons(user.Id);

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }

        [ComponentInteraction("weapon_*")]
        public async Task HandleWeaponButton(string weaponId)
        {
            LogService.Info($"[ComponentInteractions.HandleWeaponButton] Received weaponId: {weaponId}");

            if (!weaponId.Contains("weapon_"))
            {
                var correctedWeaponId = $"weapon_{weaponId}";
                weaponId = correctedWeaponId;
            }

            LogService.Info($"[ComponentInteractions.HandleWeaponButton] Handling weaponId: {weaponId}");

            var state = BattleStateSetup.GetBattleState(Context.User.Id);
            if (state == null)
            {
                await RespondAsync("❌ No active battle found.");
                return;
            }

            var weapon = GameEntityFetcher
                .RetrieveWeaponAttributes(new List<string> { weaponId })
                .FirstOrDefault();

            if (weapon == null)
            {
                await RespondAsync($"⚠️ Weapon not found: {weaponId}");
                return;
            }

            await EncounterBattleStepsSetup.HandleStepBattle(Context.Interaction, weaponId);
        }

        [ComponentInteraction("btn_attack")]
        public async Task ButtonAttackHandler()
            => await EncounterBattleStepsSetup.HandleEncounterAction(Context.Interaction, "attack", "none");

        [ComponentInteraction("battle_continue_*")]
        public async Task ContinueBattleHandler(string userIdRaw)
        {
            LogService.Info($"[ComponentInteractions.ContinueBattleHandler] Received userIdRaw: {userIdRaw}");

            if (!userIdRaw.Contains(Context.User.Id.ToString()))
            {
                await RespondAsync("⚠️ You cannot continue this battle.", ephemeral: true);
                return;
            }

            await DeferAsync();

            // --- Transition embed before calling EmbedWalk
            var user = SlashCommandHelpers.GetDiscordUser(Context, Context.User.Id);
            var player = SlashCommandHelpers.GetOrCreatePlayer(user!.Id, user.GlobalName ?? user.Username);
            var tile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint)
                       ?? SlashCommandHelpers.FindStartTile();

            await ComponentHelpers.TransitionTravelEmbed(Context, tile!);

            // --- Calling EmbedWalk for new embed
            await ReturnToWalkAsync(Context);
        }

        [ComponentInteraction("battle_flee_*")]
        public async Task FleeBattleHandler(string userIdRaw)
        {
            LogService.Info($"[ComponentInteractions.FleeBattleHandler] Received userIdRaw: {userIdRaw}");

            // Prevent other users from triggering this
            if (!userIdRaw.Contains(Context.User.Id.ToString()))
            {
                await RespondAsync("⚠️ You cannot control this battle...", ephemeral: true);
                return;
            }

            try
            {
                // Chance to flee to tile nearby : 10%
                Random rnd = new Random();
                int chance = rnd.Next(1, 100);
                LogService.Info($"[ComponentInteractions.FleeBattleHandler] int chance: {chance}...");
                if (chance <= 10)
                {
                    LogService.Info($"[ComponentInteractions.FleeBattleHandler] Flee to nearby tile...");
                    await ComponentHelpers.TransitionFleeEmbed(Context, fleeMode: "nearby");
                }
                else
                {
                    LogService.Info($"[ComponentInteractions.FleeBattleHandler] Flee to random tile...");
                    await ComponentHelpers.TransitionFleeEmbed(Context, fleeMode: "random");
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"FleeBattleHandler failed: {ex.Message}");
                await Context.Interaction.FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription("You tried to flee, but something went wrong...")
                    .WithColor(Color.Red)
                    .Build());
            }
        }

        public static async Task ReturnToWalkAsync(SocketInteractionContext context)
        {
            var user = SlashCommandHelpers.GetDiscordUser(context, context.User.Id);
            var player = SlashCommandHelpers.GetOrCreatePlayer(user!.Id, user.GlobalName ?? user.Username);

            var tile = SlashCommandHelpers.GetTileFromSavePoint(player.Savepoint)
                       ?? SlashCommandHelpers.FindStartTile();

            // ⚠️ Disable auto encounter
            await ComponentHelpers.MovePlayerAsync(context, tile!.TileId, showTravelAnimation: false, allowAutoEncounter: false);
        }
        #endregion

        #region === Walk Direction Handler ===
        //[ComponentInteraction("move:*")]
        public async Task WalkDirectionHandler(string data)
        {
            await Context.Interaction.DeferAsync();
            try
            {
                string tileId = data.Substring("move:".Length);

                if (!TestHouseLoader.TileLookup.ContainsKey(tileId))
                {
                    LogService.Error($"[ComponentInteractions.WalkDirectionHandler] ❌ Tile '{tileId}' not found");
                    await Context.Interaction.FollowupAsync($"❌ Tile '{tileId}' not found!", ephemeral: true);
                    return;
                }

                await ComponentHelpers.MovePlayerAsync(Context, tileId);
            }
            catch (Exception ex)
            {
                LogService.Error($"[ComponentInteractions.WalkDirectionHandler] Error while moving:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while moving.");
            }
        }
        #endregion

        #region === Enter Button Handler ===
        public async Task EnterTileHandler(string data)
        {
            await Context.Interaction.DeferAsync();
            try
            {
                string tileId = data.Substring("enter:".Length);
                LogService.Info($"\nReveived data: {data}\n" +
                                $"tileId: {tileId}\n");

                if (!TestHouseLoader.TileLookup.TryGetValue(tileId, out var targetTile))
                {
                    LogService.Error($"[ComponentInteractions.EnterTileHandler] ❌ Tile '{tileId}' not found");
                    await Context.Interaction.FollowupAsync($"❌ Target tile '{tileId}' not found.", ephemeral: false);
                    return;
                }

                LogService.Info($"[ComponentInteractions.EnterTileHandler] TileType: {targetTile.TileType}");

                await ComponentHelpers.MovePlayerAsync(Context, tileId, showTravelAnimation: true);
            }
            catch (Exception ex)
            {
                LogService.Error($"[ComponentInteractions.EnterTileHandler] Error:\n{ex}");
                await Context.Interaction.FollowupAsync("❌ Something went wrong while entering the new area.");
            }
        }
        #endregion
    }
}
