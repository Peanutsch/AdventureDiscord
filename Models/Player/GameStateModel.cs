using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Player
{
    public class GameStateModel
    {
        public Dictionary<string, int> Inventory { get; set; } = new Dictionary<string, int>();
    }
}
