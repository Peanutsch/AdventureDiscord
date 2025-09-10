using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adventure.Models.Text
{
    public class TextModel
    {
        public List<BattleTextModel> BattleText { get; set; } = new();
    }
}
