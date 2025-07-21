using Adventure.Models.Items;
using Adventure.Services;
using Adventure.Quest.Dice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Numerics;
using Adventure.Loaders;
using Adventure.Models.BattleState;

namespace Adventure.Quest.Battle
{
    class BattleEngineHelpers
    {
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

            // Reduce creature HP
            creature.Hitpoints -= damage;
            if (creature.Hitpoints < 0)
                creature.Hitpoints = 0;

            // Save new hitpoints value
            JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Hitpoints);

            // Player wins
            if (creature.Hitpoints <= 0)
            {
                LogService.Info($"[BattleEngine.ProcessPlayerAttack] After player attack\n" +
                                $"HP Player: {player.Hitpoints}\n" +
                                $"HP Creature: {creature.Hitpoints}");

                BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                return $"🗡️ You attacked **{creature.Name}** with your **{weapon.Name}** ({dice}) for `{damage}` damage.\n" +
                       $"Your Hitpoints: {player.Hitpoints}\n{creature.Name} Hitpoints: {creature.Hitpoints}\n" +
                       $"💀 The creature is defeated!";
            }

            // NPC survived
            LogService.Info($"[BattleEngine.ProcessPlayerAttack] After player attack\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

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

            player.Hitpoints -= damage;
            if (player.Hitpoints < 0)
                player.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] After creature attack\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

            // Save new hitpoints value
            JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Hitpoints);

            // NPC wins
            if (player.Hitpoints <= 0)
            {
                LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} rolls: {string.Join(", ", rolls)}\n" +
                                $"{creature.Name} attacked {player.Name} with his **{creature.Weapons}** ({dice}) for {damage} damage. Remaining HP: {player.Hitpoints}");

                BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
                return $"💥 **{creature.Name}** attacked you with his **{creature.Weapons}** ({dice}) for `{damage}` damage.\n" +
                       $"🧟 You have {player.Hitpoints} hitpoints\n" +
                       $"☠️ You have been defeated!";
            }

            // Player survived
            LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} rolls {dice}: {string.Join(", ", rolls)}\n" +
                $"{creature.Name} attacked {player.Name} for {damage} damage.\n" +
                $"Remaining HP: {player.Hitpoints}");

            BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);
            return $"\U0001f9df You have {player.Hitpoints} hitpoints\n💥 **{creature.Name}** attacked you ({dice}) for `{damage}` damage.\n❤️ You have `{player.Hitpoints}` HP left.";
        }

        public static void SavePlayerHitpoints(ulong userId, BattleStateModel state)
        {
            JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Hitpoints);
        }
    }
}
