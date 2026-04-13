using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Encounter;
using Adventure.Services;
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
        public static BattleStateModel GetBattleState(ulong userId)
        {
            if (!EncounterBattleStepsSetup.battleStates.TryGetValue(userId, out BattleStateModel? state))
            {
                // Load player data and inventory
                PlayerModel player = PlayerDataManager.LoadByUserId(userId);

                List<string> weaponIds = player.Weapons.Select(w => w.Id).ToList();
                List<string> armorIds = player.Armor.Select(a => a.Id).ToList();
                List<string> itemIds = player.Items.Select(i => i.Id).ToList();

                List<WeaponModel> playerWeapons = GameEntityFetcher.RetrieveWeaponAttributes(weaponIds);
                List<ArmorModel> playerArmor = GameEntityFetcher.RetrieveArmorAttributes(armorIds);
                List<ItemModel> playerItems = GameEntityFetcher.RetrieveItemAttributes(itemIds);

                // Add total ammount to Weapons
                foreach (PlayerInventoryWeaponsModel weapon in player.Weapons)
                {
                    PlayerInventoryWeaponsModel? match = player.Weapons.FirstOrDefault(w => w.Id == weapon.Id);
                    if (match != null)
                        weapon.Value = match.Value;
                }

                // Add total ammount to Armor
                foreach (PlayerInventoryArmorModel armor in player.Armor)
                {
                    PlayerInventoryArmorModel? match = player.Armor.FirstOrDefault(a => a.Id == armor.Id);
                    if (match != null)
                        armor.Value = match.Value;
                }

                // Add total ammount to Items
                foreach (PlayerInventoryItemModel item in player.Items)
                {
                    PlayerInventoryWeaponsModel? match = player.Weapons.FirstOrDefault(i => i.Id == item.Id);
                    if (match != null)
                        item.Value = match.Value;
                }


                // Create new battle state
                state = new BattleStateModel {
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

                // Set player state to InBattle and update activity time
                player.CurrentState = PlayerState.InBattle;
                player.LastActivityTime = DateTime.UtcNow;
                JsonDataManager.UpdatePlayerState(userId, PlayerState.InBattle);
                JsonDataManager.UpdatePlayerLastActivityTime(userId);
                LogService.Info($"[BattleStateSetup.GetBattleState] Player {userId} state set to InBattle, activity time updated.");
            }

            return EncounterBattleStepsSetup.battleStates.GetOrAdd(userId, state);
        }
    }
}
