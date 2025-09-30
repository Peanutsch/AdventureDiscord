using Adventure.Quest.Rolls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle.Attack
{
    public static class AttackResultHelper 
    {
        /// <summary>
        /// Converteert een HitResult naar een attackResult string
        /// die door BattleTextGenerator gebruikt kan worden.
        /// </summary>
        public static string GetAttackResult(ProcessRollsAndDamage.HitResult hitResult) {
            switch (hitResult) {
                case ProcessRollsAndDamage.HitResult.IsCriticalHit:
                    return "criticalHit";

                case ProcessRollsAndDamage.HitResult.IsValidHit:
                    return "hit";

                case ProcessRollsAndDamage.HitResult.IsCriticalMiss:
                    return "criticalMiss";

                case ProcessRollsAndDamage.HitResult.IsMiss:
                    return "miss";

                default:
                    return "hit"; // fallback
            }
        }
    }
}
