using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Events
{
    class Events
    {
        public static void GroupOfGoblins(int amount)
        {
            var text = $"You encounter {amount} goblins...";
        }
    }
}
