﻿using Adventure.Loaders;
using Adventure.Models.Attributes;
using Adventure.Models.Player;
using Adventure.Services;
using Discord;
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
        public static PlayerModel LoadByUserId(ulong userId)
        {
            string path = Path.Combine("Data", "Player", $"{userId}.json");

            if (!File.Exists(path))
            {
                LogService.Error($"[PlayerDataManager.LoadByUserId] Player file not found for userId {userId}. Returning empty PlayerModel.");
                return new PlayerModel { Id = userId };
            }

            var player = JsonDataManager.LoadObjectFromJson<PlayerModel>(path);
            return player ?? new PlayerModel { Id = userId };
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
            LogService.Info("[CreateDefaultPlayer] Attempting to load default_template_player.json");

            var defaultTemplate = JsonDataManager.LoadObjectFromJson<PlayerModel>("Data/Player/default_template_player.json");
            
            if (defaultTemplate!.Hitpoints != 50)
            {
                LogService.Error("[CreateDefaultPlayer] Error loading default template");
            }

            LogService.Info("[CreateDefaultPlayer] Finished loading default template");

            var player = defaultTemplate ?? new PlayerModel
            {
                Id = userId,
                Name = playerName,
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
               
                Weapons = new List<PlayerInventoryWeaponsModel>
                {
                    new PlayerInventoryWeaponsModel
                    {
                        Id = "",
                        Value = 0
                    }
                },

                Items = new List<PlayerInventoryItemModel>(),

                Loot = new List<PlayerInventoryItemModel>()
            };

            player.Id = userId;
            player.Name = GenerateUniquePlayerName(playerName);

            SaveByUserId(userId, player);
            return player;
        }
    }
}
