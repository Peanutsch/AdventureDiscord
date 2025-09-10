using Adventure.Models.Items;
using Adventure.Models.Text;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Loaders
{
    public static class TextLoader
    {
        public static List<TextModel> Load()
        {
            var combinedText = new List<TextModel>();

            var battleText = JsonDataManager.LoadListFromJson<TextModel>("Data/Text/battletext.json");
            if (battleText != null)
            {
                LogService.Info($"[TextLoader] > Adding [battleText]: {battleText.Count} to GameData.Text");
                combinedText.AddRange(battleText);
            }

            return combinedText;
        }
    }
}
