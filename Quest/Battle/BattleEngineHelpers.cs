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
            var (damage, rolls) = DiceRoller.RollWithDetails(diceCount, diceValue);
            var dice = $"{diceCount}d{diceValue}";
            var totalDamage = damage + playerStrength;

            state.Damage = totalDamage;
            state.LastUsedWeapon = weapon.Name!;
            state.PreCreatureHP = creature.Hitpoints + totalDamage;

            // Reduce creature HP
            creature.Hitpoints -= totalDamage;
            if (creature.Hitpoints < 0)
                creature.Hitpoints = 0;

            // Save new hitpoints value
            JsonDataManager.UpdatePlayerHitpointsInJson(userId, state.Player.Name!, state.Player.Hitpoints);

            // Player wins
            if (creature.Hitpoints <= 0)
            {
                LogService.Info($"[BattleEngine.ProcessPlayerAttack] After player attack\n" +
                                $"HP {player.Name}: {player.Hitpoints}\n" +
                                $"HP Creature: {creature.Hitpoints}");

                // Set step in Battleengine
                BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);

                return $"🎲 {player.Name} rolls: {string.Join(", ", rolls)}\n\n" +
                       $"🗡️ **{player.Name}** attacked **{creature.Name}** with **{weapon.Name}** ({dice}) for `{damage}` damage.\n\n" +
                       $"🎯  Total damage = damage ({damage}) + STR({playerStrength}) = {totalDamage}\n" +
                       //$"{player.Name} HP: {player.Hitpoints}\n{creature.Name} HP: {creature.Hitpoints}\n" +
                       $"💀 **{creature.Name}** is defeated!";
            }

            // NPC survived
            LogService.Info($"[BattleEngine.ProcessPlayerAttack] After player attack\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

            LogService.Info($"[BattleEngine.ProcessPlayerAttack] Player rolls: {string.Join(", ", rolls)}\n" +
                            $"{player.Name} attacked {creature.Name} for {damage} damage. Remaining HP: {creature.Hitpoints}");

            BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);

            return $"🎲 **{player.Name}** rolls: {string.Join(", ", rolls)}\n" +
                   $"🗡️ **{player.Name}** attacked **{creature.Name}** with **[{weapon.Name}]** ({dice}) for `{damage}` damage.\n" +
                   $"🎯  Total damage = damage ({damage}) + STR({playerStrength}) = {totalDamage}\n\n" +
                   //$"Player {player.Name} HP: {player.Hitpoints}\n{creature.Name} Hitpoints: {creature.Hitpoints}\n" +
                   $"🧟 **{creature.Name}** is still standing with {creature.Hitpoints} HP!";
        }

        /// <summary>
        /// Processes the creature's attack on the player.
        /// </summary>
        public static string ProcessCreatureAttack(ulong userId, WeaponModel weapon)
        {
            var state = BattleEngine.GetBattleState(userId);
            var creature = state.Creatures;
            var player = state.Player;
            var creatureStrength = creature.Attributes.Strength;

            var diceCount = weapon.Damage.DiceCount;
            var diceValue = weapon.Damage.DiceValue;
            var (damage, rolls) = DiceRoller.RollWithDetails(diceCount, diceValue);
            var dice = $"{diceCount}d{diceValue}";
            var totalDamage = damage + creatureStrength;

            state.Damage = totalDamage;
            state.LastUsedWeapon = weapon.Name!;
            state.PrePlayerHP = player.Hitpoints + totalDamage;

            player.Hitpoints -= totalDamage;
            if (player.Hitpoints < 0)
                player.Hitpoints = 0;

            LogService.Info($"[BattleEngine.ProcessCreatureAttack] After creature attack\n" +
                            $"HP Player: {player.Hitpoints}\n" +
                            $"HP Creature: {creature.Hitpoints}");

            // Save new hitpoints value
            JsonDataManager.UpdatePlayerHitpointsInJson(userId, player.Name!,state.Player.Hitpoints);

            // NPC wins
            if (player.Hitpoints <= 0)
            {
                LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} rolls: {string.Join(", ", rolls)}\n" +
                                $"{creature.Name} attacked {player.Name} with his **{creature.Weapons}** ({dice}) for {damage} damage. Remaining HP: {player.Hitpoints}" +
                                $"Player {player.Name} is still standing!");

                // Set step in Battleengine
                BattleEngine.SetStep(userId, BattleEngine.StepEndBattle);

                return $"🎲 **{creature.Name}** rolls: {string.Join(", ", rolls)}\n\n" +
                       $"💥 **{creature.Name}** attacked {player.Name} with his **[{creature.Weapons}]** ({dice}) for `{damage}` damage.\n" +
                       $"🎯 Total damage = damage ({damage}) + {creature.Name}'s STR({creatureStrength}) = {totalDamage}\n\n" +
                       //$"🧟 {player.Name} have `{player.Hitpoints}` hitpoints\n" +
                       $"**☠️ **{player.Name}** have been defeated!**";
            }

            // Player survived
            LogService.Info($"[BattleEngine.ProcessCreatureAttack] {creature.Name} rolls {dice}: {string.Join(", ", rolls)}\n" +
                $"{creature.Name} attacked {player.Name} with his {weapon.Name} ({dice}) for `{damage}` damage.\n" +
                $"Total damage = damage ({damage}) + your STR({creatureStrength}) = {totalDamage}\n" +
                $"Remaining HP: {player.Hitpoints}");

            // Set step in Battleengine
            BattleEngine.SetStep(userId, BattleEngine.StepPostBattle);

            return $"🎲 **{creature.Name}** rolls {dice}: {string.Join(", ", rolls)}\n" +
                   $"💥 **{creature.Name}** attacked {player.Name} with his **[{weapon.Name}]** ({dice}) for `{damage}` damage.\n" +
                   $"🎯 Total damage = damage ({damage}) + {creature.Name}'s STR({creatureStrength}) = {totalDamage}\n\n" +
                   $"🧟 **{player.Name}** is still standing with {player.Hitpoints} HP!\n";
        }
    }
}
