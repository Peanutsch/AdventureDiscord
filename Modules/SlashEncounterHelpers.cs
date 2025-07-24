using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.Creatures;
using Adventure.Models.Player;
using Adventure.Quest.Battle;
using Adventure.Services;
using Discord.Interactions;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Modules
{
    public static class SlashEncounterHelpers
    {
        public static IUser? GetDiscordUser(SocketInteractionContext context, ulong userId)
        {
            return context.Client.GetUser(userId);
        }

        public static PlayerModel GetOrCreatePlayer(ulong userId, string playerName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[SlashEncounterHelpers.GetOrCreatePlayer] No player file found. Creating for {playerName} ({userId})");
                return PlayerDataManager.CreateDefaultPlayer(userId, playerName);
            }

            var player = PlayerDataManager.LoadByUserId(userId);
            return player ?? PlayerDataManager.CreateDefaultPlayer(userId, playerName);
        }

        /*
        public static void EnsureInventoryLoaded(ulong userId)
        {
            if (GameData.Inventory == null)
            {
                LogService.Info("[SlashEncounterHelpers.EnsureInventoryLoaded] Inventory not loaded, reloading...");
                GameData.Inventory = InventoryLoader.Load();
            }

            InventoryStateService.LoadInventory(userId);
        }
        */

        public static void SetupBattleState(ulong userId, CreaturesModel creature)
        {
            BattleEngine.SetCreature(userId, creature);
            BattleEngine.SetStep(userId, "start");
        }

        public static ComponentBuilder BuildEncounterButtons()
        {
            return new ComponentBuilder()
                .WithButton("Attack", "btn_attack", ButtonStyle.Danger)
                .WithButton("Flee", "btn_flee", ButtonStyle.Secondary);
        }
    }
}
