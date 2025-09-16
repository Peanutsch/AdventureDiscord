using Adventure.Models.BattleState;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Helpers
{
    public class TrackHP
    {
        public enum TargetType
        {
            Player,
            NPC
        }

        public static void GetAndSetHPStatus(int startHP, int currentHP, TargetType target, BattleStateModel battleState)
        {
            string result;

            if (startHP <= 0)
            {
                if (target == TargetType.Player)
                    battleState.StateOfPlayer = "UNKNOWN: startHP <= 0";
                else
                    battleState.StateOfNPC = "UNKNOWN: startHP <= 0";

                return;
            }

            double percentHP = (double)currentHP / startHP * 100;

            if (percentHP > 50 && percentHP <= 75)
                result = "Lightly Wounded";
            else if (percentHP > 25 && percentHP <= 50)
                result = "Heavily Wounded";
            else if (percentHP > 0 && percentHP <= 25)
                result = "Deadly Wounded";
            else if (percentHP <= 0)
                result = "💀 DEAD";
            else
                result = "Healthy";

            if (target == TargetType.Player)
            {
                battleState.StateOfPlayer = result;
                battleState.PercentageHpNpc = (int)Math.Round(percentHP);
            }
            else
            {
                battleState.StateOfNPC = result;
            }
                
        }
    }
}
