using Adventure.Data;
using Adventure.Loaders;
using Adventure.Models.BattleState;
using Adventure.Models.Items;
using Adventure.Models.NPC;
using Adventure.Models.Player;
using Adventure.Quest.Encounter;
using Adventure.Services;
using System;

namespace Adventure.Quest.Battle.BattleEngine
{
    public class BattleStateSetup
    {
        /// <summary>
        /// Retrieves or initializes the battle session for the user.
        /// </summary>
        public static BattleSession GetBattleSession(ulong userId)
        {
            if (!EncounterBattleStepsSetup.battleSessions.TryGetValue(userId, out BattleSession? session))
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


                // Create new battle session with separated context and state
                session = new BattleSession
                {
                    Context = new BattleContext
                    {
                        Player = player,
                        Npc = new NpcModel(),
                        PlayerWeapons = playerWeapons,
                        PlayerArmor = playerArmor,
                        Items = playerItems,
                        NpcWeapons = new List<WeaponModel>(),
                        NpcArmor = new List<ArmorModel>()
                    },
                    State = new BattleRuntimeState
                    {
                        PreHpPlayer = player.Hitpoints,
                        PreHpNPC = 0,
                        LastUsedWeapon = "",
                        TotalDamage = 0,
                        EmbedColor = Discord.Color.Red
                    }
                };

                // Set player state to InBattle and update activity time
                player.CurrentState = PlayerState.InBattle;
                player.LastActivityTime = DateTime.UtcNow;
                JsonDataManager.UpdatePlayerState(userId, PlayerState.InBattle);
                JsonDataManager.UpdatePlayerLastActivityTime(userId);
                LogService.Info($"[BattleStateSetup.GetBattleSession] Player {userId} state set to InBattle, activity time updated.");
            }
            else
            {
                // Session exists - sync NPC HP from multiplayer encounter if applicable
                if (!string.IsNullOrEmpty(session.State.EncounterTileId))
                {
                    var encounterData = Adventure.Services.ActiveEncounterTracker.GetEncounter(session.State.EncounterTileId);
                    if (encounterData != null)
                    {
                        // Sync current HP from shared encounter
                        session.State.CurrentHitpointsNPC = encounterData.CurrentHitpoints;
                    }
                }
            }

            return EncounterBattleStepsSetup.battleSessions.GetOrAdd(userId, session);
        }

        /// <summary>
        /// LEGACY: Backward compatibility wrapper for old code.
        /// Use GetBattleSession() in new code instead.
        /// </summary>
        [Obsolete("Use GetBattleSession() instead")]
        public static BattleStateModel GetBattleState(ulong userId)
        {
            BattleSession session = GetBattleSession(userId);
            return BattleStateModel.FromSession(session);
        }
    }
}
