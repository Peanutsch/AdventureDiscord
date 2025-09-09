using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.NpcHelpers
{
    class NpcDisplayCR
    {
        public static string DisplayCR(double cr)
        {
            if (cr == 0.125) return "1/8";
            else if (cr == 0.25) return "1/4";
            else if (cr == 0.5) return "1/2";
            else return cr.ToString();
        }
    }
}
