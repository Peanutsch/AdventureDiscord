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

        public static string GetHPStatus(int startHP, int currentHP, TargetType target, BattleStateModel battleState)
        {
            if (startHP <= 0)
            {
                return target == TargetType.Player
                    ? battleState.StateOfPlayer = ""
                    : battleState.StateOfNPC = "";
            }

            string result;
            double percentHP = (double)currentHP / startHP * 100;

            if (percentHP > 100)
                result = "Healthy";
            else if (percentHP <= 75)
                result = "Lightly Wounded";
            else if (percentHP <= 50)
                result = "Heavily Wounded";
            else if (percentHP > 0 && percentHP <= 25)
                result = "Deadly Wounded";
            else
                result = "Dead";

            if (target == TargetType.Player)
            {
                battleState.StateOfPlayer = result;
            }
            else
            {
                LogService.Info($"[TrackHP.GetHPStatus] StateOfNPC BEFORE: {percentHP}% {battleState.StateOfNPC}");
                battleState.StateOfNPC = result;
                LogService.Info($"[TrackHP.GetHPStatus] StateOfNPC UPDATED: {percentHP}% {battleState.StateOfNPC}\n");
            }

            return result;
        }
    }
}
