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


        public static string GetAndSetHPStatus(int startHP, int currentHP, TargetType target, BattleStateModel battleState)
        {
            if (startHP <= 0)
            {
                return target == TargetType.Player
                    ? battleState.StateOfPlayer = ""
                    : battleState.StateOfNPC = "";
            }

            string result;
            double percentHP = (double)currentHP / startHP * 100;
            //double percentHP = (double)battleState.CurrentHitpointsNPC / battleState.HitpointsAtStartNPC * 100;
            //LogService.Info($"[TrackHP.GetHPStatus]\npercentHP = (double){battleState.CurrentHitpointsNPC} / {battleState.HitpointsAtStartNPC} * 100 = {percentHP}");
            LogService.Info($"[TrackHP.GetHPStatus]\npercentHP = (double){currentHP} / {startHP} * 100 = {percentHP}");

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
            }
            else
            {
                LogService.Info($"[TrackHP.GetHPStatus] StateOfNPC BEFORE: {percentHP}% {battleState.StateOfNPC}");
                //battleState.StateOfNPC = result;
                LogService.Info($"[TrackHP.GetHPStatus] StateOfNPC UPDATED: {percentHP}% {battleState.StateOfNPC}\n");
            }

            LogService.Info($"[[TrackHP.GetHPStatus]] Returning {result}");
            return result;
        }
    }
}
