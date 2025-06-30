using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Player
{
    public class GameStateModel
    {
        public Dictionary<int, string> Inventory { get; set; } = new Dictionary<int, string>();
    }
}
