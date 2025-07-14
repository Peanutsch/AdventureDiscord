using Adventure.Loaders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Models.Inventory
{
    public static class InventoryStateService
    {
        private static readonly ConcurrentDictionary<ulong, InventoryStateModel> playerStates = new();

        public static InventoryStateModel GetState(ulong userId)
        {
            if (!playerStates.ContainsKey(userId))
                playerStates[userId] = new InventoryStateModel();

            return playerStates[userId];
        }

        public static void LoadInventory(ulong userId)
        {
            var state = GetState(userId);
            state.Inventory.Clear();

            var defaultInventory = InventoryLoader.Load();
            if (defaultInventory != null)
            {
                foreach (var item in defaultInventory)
                {
                    state.Inventory[item.Item] = item.Value;
                }
            }
        }

        public static void ResetInventory(ulong userId)
        {
            var state = GetState(userId);
            state.Inventory.Clear();
            state.Inventory.Add("Shortsword", 1);
            state.Inventory.Add("Dagger", 1);
        }
    }
}
