using Newtonsoft.Json;
using System;


namespace Adventure.Loaders
{
    public static class JsonDataLoader
    {
        public static List<T>? LoadListFromJson<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<T>>(json);
        }

        public static T? LoadObjectFromJson<T>(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
