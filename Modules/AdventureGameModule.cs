using Adventure.Models.Enviroment;
using Adventure.Services;
using Adventure.Models.Player;
using Discord.Interactions;
using System.Collections.Concurrent;
using Discord;
using Adventure.Events.EventService;
using Adventure.Quest.Battle;
using Adventure.Loaders;
using Adventure.Data;

namespace Adventure.Modules
{
    public class AdventureGameModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Store player progress using a thread-safe dictionary
        private static readonly ConcurrentDictionary<ulong, InventoryStateModel> playerStates = new();

        /// <summary>
        /// Starts the player's adventure and initializes their state.
        /// </summary>
        [SlashCommand("start", "Start the adventure.")]
        public async Task SlashCommandStartHandler()
        {
            var user = Context.Client.GetUser(Context.User.Id); // Of gewoon: Context.User
            if (user != null)
            {
                string username = user.Username;
                string displayName = user.GlobalName ?? user.Username;
                LogService.Info($"[Encounter] Discord user '{displayName}' (ID: {user.Id})");
            }

            // Defer the response to prevent the "No response" error
            await DeferAsync();

            LogService.SessionDivider('=', "START");
            LogService.Info("[AdventureGameModule.SlashCommandStartHandler] > Slash Command /start is executed");

            // Reset inventory to basic inventory: Shordsword and Dagger
            InventoryStateService.LoadInventory(Context.User.Id);

            // Send a follow-up response after the processing is complete.
            //await FollowupAsync("Slash Command /start is executed");
            await FollowupAsync("Your adventure has begun!");
        }

        [SlashCommand("inventory", "Show your inventory")]
        public async Task SlashCommandInventoryHandler()
        {
            await DeferAsync();

            if (!playerStates.TryGetValue(Context.User.Id, out var gameState))
            {
                await FollowupAsync("You haven't started your adventure yet. Use /start.");
                return;
            }

            LogService.Info("[AdventureGameModule.SlashCommandInventoryHandler] > Slash Command /inventory is executed");

            EmbedBuilder embed = InventoryEmbedBuilder.BuildInventoryEmbed(gameState.Inventory);

            await FollowupAsync(embed: embed.Build());

        }

        // Trigger encounter for testing
        [SlashCommand("encounter", "Triggers a random encounter")]
        public async Task SlashCommandEncounterHandler()
        {
            #region CONTROL USER DATA
            var user = Context.Client.GetUser(Context.User.Id);
            string displayName = user.GlobalName ?? user.Username;

            if (user == null)
            {
                LogService.Error("[/Encounter] Could not find user.");
                await RespondAsync("⚠️ An error occured file loading your userdata...");
                return;
            }

            LogService.DividerParts(1, "Slashcommand: Encounter");
            LogService.Info($"[/Encounter] Slash command triggered by {displayName} (userId: {user!.Id})");
            #endregion CONTROL USER DATA

            // Verify player
            string relativePath = $"Data/Player/{user.Id}.json";
            string filePath = Path.Combine(AppContext.BaseDirectory, relativePath);
            PlayerModel? player;

            if (!File.Exists(filePath))
            {
                LogService.Error($"[/Encounter] No existing player file found for {displayName}. Creating Player Model.");
                player = PlayerDataManager.CreateDefaultPlayer(user.Id, displayName);
            }
            else
            {
                player = PlayerDataManager.LoadByUserId(user.Id);
                if (player == null)
                {
                    LogService.Error($"[/Encounter] Failed to load existing player data. Recreating default for {displayName}.");
                    player = PlayerDataManager.CreateDefaultPlayer(user.Id, displayName);
                }
            }

            player!.Id = user.Id;
            LogService.Info($"[/Encounter] Player '{player.Name}' loaded with ID: {user.Id}");

            // Load inventory
            LogService.Info("[/Encounter] Checking Inventory...");
            if (GameData.Inventory == null)
            {
                LogService.Info("[/Encounter] Reload Inventory...");
                GameData.Inventory = InventoryLoader.Load();
                InventoryStateService.LoadInventory(Context.User.Id);
            }

            await DeferAsync();

            // Randomizer NPC
            var creature = EncounterService.CreatureRandomizer();
            if (creature == null)
            {
                await FollowupAsync("⚠️ Could not pick a random creature.");
                LogService.Error("[AdventureGameModule.SlashCommandEncounterHandler] > No creature could be picked.");
                return;
            }

            LogService.Info($"[/Encounter] Creature '{creature.Name}' chosen for userId: {user.Id}");

            BattleEngine.SetCreature(user.Id, creature);     // Update BattleState 
            BattleEngine.SetStep(Context.User.Id, "start"); // Reset step from GameEngine.HandleEncounterAction to [start]

            var embed = EncounterService.GetRandomEncounter(creature);

            var buttons = new ComponentBuilder()
                .WithButton("Attack", "btn_attack", ButtonStyle.Danger)
                .WithButton("Flee", "btn_flee", ButtonStyle.Secondary);

            await FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
    }
}