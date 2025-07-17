using Adventure.Models.Player;
using Adventure.Services;
using System;
using System.Text.Json;


namespace Adventure.Loaders
{
    public static class JsonDataManager
    {
        public static List<T>? LoadListFromJson<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json);
        }

        public static T? LoadObjectFromJson<T>(string filePath)
        {
            LogService.Info($"[JsonDataManager.LoadObjectFromJson] Method LoadObjectFromJson is called.\nparam filepath: {filePath}");

            var json = File.ReadAllText(filePath);

            LogService.Info($"[JsonDataManager.LoadObjectFromJson] Returning object\n{json}");

            return JsonSerializer.Deserialize<T>(json);
        }

        public static void SaveToJson(ulong userId, PlayerModel player)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Player", $"{userId}.json");
            string? directory = Path.GetDirectoryName(filePath);

            LogService.Info($"[JsonDataManager.SaveToJson] Directory: {directory}");
            try
            {
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(player, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                LogService.Error($"[SaveToJson] Error saving player data for {userId}:\n{ex.Message}");
            }
        }
    }
}
