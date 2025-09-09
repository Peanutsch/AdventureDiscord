using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.NpcHelpers
{
    class HitDiceCR
    {
        public static (int diceCount, int diceValue) GetHitDieByCR(double cr)
        {
            if (cr <= 0.125) return (2, 6);    
            else if (cr <= 0.25) return (2, 8);   
            else if (cr <= 0.5) return (2, 8);    
            else if (cr <= 1) return (2, 8);    
            else if (cr <= 2) return (3, 8);    
            else if (cr <= 3) return (4, 10);   
            else if (cr <= 5) return (6, 10);   
            else if (cr <= 10) return (12, 10);
            else return (20, 12);
        }
    }
}
