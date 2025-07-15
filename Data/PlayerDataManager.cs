using Adventure.Loaders;
using Adventure.Models.Attributes;
using Adventure.Models.Player;
using Adventure.Services;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Data
{
    public static class PlayerDataManager
    {
        public static PlayerModel? LoadByUserId(ulong userId)
        {
            string path = $"Data/Player/{userId}.json";
            return JsonDataManager.LoadObjectFromJson<PlayerModel>(path);
        }

        public static List<PlayerModel>? LoadAll()
        {
            string directoryPath = "Data/Player";

            if (!Directory.Exists(directoryPath))
                return new List<PlayerModel>();

            var players = new List<PlayerModel>();
            var files = Directory.GetFiles(directoryPath, "*.json");

            foreach (var file in files)
            {
                var player = JsonDataManager.LoadObjectFromJson<PlayerModel>(file);
                if (player != null)
                {
                    players.Add(player);
                }
            }

            return players;
        }

        public static string GenerateUniquePlayerName(string baseName)
        {
            var players = LoadAll();
            var existingNames = players?.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

            string uniqueName = baseName;
            var rand = new Random();

            while (existingNames.Contains(uniqueName))
            {
                LogService.Error($"[PlayerDataManager.GenerateUniquePlayerName] Player Name {uniqueName} already exist. Creating unique player name...");
                uniqueName = $"{baseName}#{rand.Next(1000, 9999)}";

                LogService.Error($"[PlayerDataManager.GenerateUniquePlayerName] Unique Player Name: {uniqueName}...");
            }

            return uniqueName;
        }

        public static void SaveByUserId(ulong userId, PlayerModel player)
        {
            JsonDataManager.SaveToJson(userId, player);
        }

        public static PlayerModel CreateDefaultPlayer(ulong userId, string playerName)
        {
            var defaultTemplate = JsonDataManager.LoadObjectFromJson<PlayerModel>("Data/Player/default_template_player.json");
            var player = defaultTemplate ?? new PlayerModel
            {
                Hitpoints = 50,
                MaxCarry = 70,
                Attributes = new AttributesModel
                {
                    Strength = 10,
                    Dexterity = 14,
                    Constitution = 10,
                    Intelligence = 10,
                    Wisdom = 11,
                    Charisma = 10
                },
                Weapons = new() { "weapon_short_sword", "weapon_dagger" }
            };

            player.Id = userId;
            player.Name = GenerateUniquePlayerName(playerName);

            SaveByUserId(userId, player);
            return player;
        }
    }
}
