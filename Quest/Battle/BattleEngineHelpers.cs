using Adventure.Helpers;
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
        //private const string StepPostBattle = "post_battle";

        /// <summary>
        /// Processes the player's attack and applies damage to the creature.
        /// </summary>
        public static string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
        {
            var state = BattleEngine.GetBattleState(userId);
            var creature = state.Creatures;
            var player = state.Player;
            var playerStrength = state.Player.Attributes.Strength;
            var diceCount = weapon.Damage.DiceCount;
            var diceValue = weapon.Damage.DiceValue;
            var dice = $"{diceCount}d{diceValue}";

            var (damage, rolls) = DiceRoller.RollWithDetails(diceCount, diceValue);

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] Before creature.Hitpoints -= damage\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

            // Reduce creature HP
            creature.Hitpoints -= damage;
            if (creature.Hitpoints < 0)
                creature.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] After creature.Hitpoints -= damage\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

            if (creature.Hitpoints <= 0)
            {
                BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** ({dice}) for `{damage}` damage.\n" +
                       $"Your Hitpoints: {player.Hitpoints}\n{creature.Name} Hitpoints: {creature.Hitpoints}\n" +
                       $"💀 The creature is defeated!";
            }

            LogService.Info($"[BattleEngine.ProcessPlayerAttack] Player rolls: {string.Join(", ", rolls)}\n" +
                $"{state.Player.Name} attacked {creature.Name} for {damage} damage. Remaining HP: {creature.Hitpoints}");

            return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** ({dice}) for `{damage}` damage.\n" +
                   $"Your Hitpoints: {player.Hitpoints}\n{creature.Name} Hitpoints: {creature.Hitpoints}\n" +
                   $"🧟 {creature.Name} has `{creature.Hitpoints}` HP left.";
        }

        /// <summary>
        /// Processes the creature's attack on the player.
        /// </summary>
        public static string ProcessCreatureAttack(ulong userId, WeaponModel weapon)
        {
            var state = BattleEngine.GetBattleState(userId);
            var player = state.Player;
            var creature = state.Creatures;
            var creatureStrenght = state.Creatures.Attributes.Strength;
            var diceCount = weapon.Damage.DiceCount;
            var diceValue = weapon.Damage.DiceValue;
            var dice = $"{diceCount}d{diceValue}";

            var (damage, rolls) = DiceRoller.RollWithDetails(diceCount, diceValue);

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] Before player.Hitpoints -= damage\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

            player.Hitpoints -= damage;
            if (player.Hitpoints < 0)
                player.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] After player.Hitpoints -= damage\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");


            if (player.Hitpoints <= 0)
            {
                LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} rolls: {string.Join(", ", rolls)}\n" +
                                $"{creature.Name} attacked {player.Name} with his **{creature.Weapons}** ({dice}) for {damage} damage. Remaining HP: {player.Hitpoints}");

                BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                return $"💥 **{creature.Name}** attacked you with his **{creature.Weapons}** ({dice}) for `{damage}` damage.\n" +
                       $"🧟 You have {player.Hitpoints} hitpoints\n" +
                       $"☠️ You have been defeated!";
            }

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} rolls {dice}: {string.Join(", ", rolls)}\n" +
                $"{creature.Name} attacked {player.Name} for {damage} damage.\n" +
                $"Remaining HP: {player.Hitpoints}");

            return $"\U0001f9df You have {player.Hitpoints} hitpoints\n💥 **{creature.Name}** attacked you ({dice}) for `{damage}` damage.\n❤️ You have `{player.Hitpoints}` HP left.";
        }
    }
}
