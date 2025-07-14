using Adventure.Models.Items;
using Adventure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle
{
    class BattleEngineHelpers
    {
        private const string StepPostBattle = "post_battle";

        /// <summary>
        /// Processes the player's attack and applies damage to the creature.
        /// </summary>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            var state = BattleEngine.GetBattleState(userId);
            var creature = state.Creatures;
            var random = new Random();

            int damage = 0; // Placeholder — logic for actual damage calculation to be implemented

            // Reduce creature HP
            creature.Hitpoints -= damage;
            if (creature.Hitpoints < 0)
                creature.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessPlayerAttack] {state.Player.Name} attacked {creature.Name} for {damage} damage. Remaining HP: {creature.Hitpoints}");

            if (creature.Hitpoints <= 0)
            {
                BattleEngine.SetStep(userId, StepPostBattle);
                return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** for `{damage}` damage.\n💀 The creature is defeated!";
            }

            return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** for `{damage}` damage.\n🧟 {creature.Name} has `{creature.Hitpoints}` HP left.";
        }

        /// <summary>
        /// Processes the creature's attack on the player.
        /// </summary>
        public static string ProcessCreatureAttack(ulong userId)
        {
            var state = BattleEngine.GetBattleState(userId);
            var player = state.Player;
            var creature = state.Creatures;

            var random = new Random();
            //int damage = random.Next(5, 11); // Creature deals between 5 and 10 damage
            int damage = 0;

            player.Hitpoints -= damage;
            if (player.Hitpoints < 0)
                player.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} attacked {player.Name} for {damage} damage. Remaining HP: {player.Hitpoints}");

            if (player.Hitpoints <= 0)
            {
                BattleEngine.SetStep(userId, StepPostBattle);
                return $"💥 **{creature.Name}** attacked you for `{damage}` damage.\n☠️ You have been defeated!";
            }

            return $"💥 **{creature.Name}** attacked you for `{damage}` damage.\n❤️ You have `{player.Hitpoints}` HP left.";
        }
    }
}
