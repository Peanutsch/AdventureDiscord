using Adventure.Models;
using Adventure.Services;
using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Loaders
{
    public static class InventoryLoader
    {
        public static List<InventoryModel>? Load()
        {
            var inventory = JsonDataLoader.LoadListFromJson<InventoryModel>("Data/Inventory/inventory.json");
            LogService.Info($"[InventoryLoader] > Adding [Inventory]: {inventory!.Count} to GameData.Inventory");

            return JsonDataLoader.LoadListFromJson<InventoryModel>("Data/Inventory/inventory.json");
        }
    }
}
