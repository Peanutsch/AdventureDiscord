using Adventure.Data;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Quest.Encounter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Quest.Battle.BattleEngine
{
    public class BattleStateSetup
    {
        /// <summary>
        /// Retrieves or initializes the battle state for the user.
        /// </summary>
        public static BattleState GetBattleState(ulong userId)
        {
            if (!EncounterBattleStepsSetup.battleStates.TryGetValue(userId, out var state))
            {
                // Load player data and inventory
                var player = PlayerDataManager.LoadByUserId(userId);

                var weaponIds = player.Weapons.Select(w => w.Id).ToList();
                var armorIds = player.Armor.Select(a => a.Id).ToList();
                var itemIds = player.Items.Select(i => i.Id).ToList();

                var playerWeapons = GameEntityFetcher.RetrieveWeaponAttributes(weaponIds);
                var playerArmor = GameEntityFetcher.RetrieveArmorAttributes(armorIds);
                var playerItems = GameEntityFetcher.RetrieveItemAttributes(itemIds);

                // Add total ammount to Weapons
                foreach (var weapon in player.Weapons)
                {
                    var match = player.Weapons.FirstOrDefault(w => w.Id == weapon.Id);
                    if (match != null)
                        weapon.Value = match.Value;
                }

                // Add total ammount to Armor
                foreach (var armor in player.Armor)
                {
                    var match = player.Armor.FirstOrDefault(a => a.Id == armor.Id);
                    if (match != null)
                        armor.Value = match.Value;
                }

                // Add total ammount to Items
                foreach (var item in player.Items)
                {
                    var match = player.Weapons.FirstOrDefault(i => i.Id == item.Id);
                    if (match != null)
                        item.Value = match.Value;
                }


                // Create new battle state
                state = new BattleState {
                    Player = player,
                    Npc = new NpcModel(),
                    PlayerWeapons = playerWeapons,
                    PlayerArmor = playerArmor,
                    Items = playerItems,
                    NpcWeapons = new List<WeaponModel>(),
                    NpcArmor = new List<ArmorModel>(),
                    PreHpPlayer = player.Hitpoints,
                    PreHpNPC = 0,
                    LastUsedWeapon = "",
                    TotalDamage = 0,
                    EmbedColor = Discord.Color.Red
                };
            }

            return EncounterBattleStepsSetup.battleStates.GetOrAdd(userId, state);
        }
    }
}
