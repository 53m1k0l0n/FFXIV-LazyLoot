using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using HtmlAgilityPack;
using LazyLoot.Attributes;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;

namespace LazyLoot.Commands
{
#if DEBUG
    public class DevCommand : BaseCommand
    {
        [Command("/lldev", "Dev Command", false)]
        public async void DevCommandRun(string command, string arguments)
        {
            var itemSheet = Services.Service.Data.Excel.GetSheet<Item>()!.Where(x => x.ItemAction.Value.Type == 29153 && x.Name.RawString.Contains("Arms") && x.IsUnique);

            foreach (var item in itemSheet)
            {
                var itemAction = item.ItemAction.Value;
                if (itemAction == null) continue;

                if (item.Name.RawString.IsNullOrEmpty() || itemAction.Type != 29153 || !item.Name.RawString.Contains("Arms")) continue;

                PluginLog.Information($"Name: {item.Name} ID : {item.RowId} Row: {itemAction.RowId} Type  : {itemAction.Type} EquipSlotCategory.Row : {item.EquipSlotCategory.Row} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");

                ////var test = Regex.Replace(item.Name.RawString, "Gladiator's|Gladiator|Paladin's|Paladin|Arms|[(-)]\\D*\\d*\\D", string.Empty);

                ////////var test = item.Name.RawString.Replace("Gladiator's", string.Empty);
                ////////test = test.Replace("Gladiator", string.Empty);
                ////////test = test.Replace("Paladin's", string.Empty);
                ////////test = test.Replace("Paladin", string.Empty);
                ////////test = test.Replace("Arms", string.Empty);
                ////////test = Regex.Replace(test, "[(-)]\\D*\\d*\\D", string.Empty);
                ////test = test.Trim();

                try
                {
                    var client = new HttpClient();
                    var response = await client.GetAsync($"https://ffxiv.consolegameswiki.com/wiki/{item.Name}");
                    var pageContents = await response.Content.ReadAsStringAsync();
                    var pageDocument = new HtmlDocument();
                    pageDocument.LoadHtml(pageContents);
                    var text = pageDocument.DocumentNode
                        .SelectSingleNode("(//div[contains(@class,'mw-parser-output')]//ul[2]//li)[1]").InnerText.Replace("&#160;", string.Empty).Trim();
                    var text2 = pageDocument.DocumentNode
                        .SelectSingleNode("(//div[contains(@class,'mw-parser-output')]//ul[2]//li)[2]").InnerText.Replace("&#160;", string.Empty).Trim();

                    var itemResult = Services.Service.Data.Excel.GetSheet<Item>()!.First(x => x.Name == text);
                    var itemResult2 = Services.Service.Data.Excel.GetSheet<Item>()!.First(x => x.Name == text2);

                    PluginLog.Error($"{itemResult.Name}");
                    PluginLog.Error($"{itemResult2.Name}");

                    if (GetItemCount(itemResult.RowId) != 0)
                    {
                        PluginLog.Error($"Found : {itemResult.Name}");
                    }

                    if (GetItemCount(itemResult2.RowId) != 0)
                    {
                        PluginLog.Error($"Found : {itemResult2.Name}");
                    }
                }
                catch
                {
                }

                ////var itemSheet2 = Services.Service.Data.Excel.GetSheet<Item>()!.Where(x => x.ClassJobCategory.Value.PLD && x.ClassJobCategory.Value.GLA && x.IsUnique && x.EquipSlotCategory.Row == 1 && (x.Name.RawString.StartsWith(test) || x.Name.RawString.EndsWith(test))).ToList();

                ////foreach (var item2 in itemSheet2)
                ////{
                ////    var itemAction2 = item2.ItemAction.Value;
                ////    var item3 = Services.Service.Data.Excel.GetSheet<Item>()!.GetRow(item2.RowId + 13);
                ////    var itemAction3 = item3.ItemAction.Value;

                ////    ////if (!item2.Name.RawString.Contains(test)) continue;
                ////    ////if (item2.Name.RawString.Contains("Seeing")) continue;
                ////    ////if (!item2.Name.RawString.StartsWith(test) && !item2.Name.RawString.EndsWith(test)) continue;
                ////    ////if (!item2.ClassJobCategory.Value.PLD || !item2.ClassJobCategory.Value.GLA || item2.EquipSlotCategory.Row < 1 || item2.EquipSlotCategory.Row > 2) continue;

                ////    PluginLog.Information($"Name: {item.Name} ID : {item.RowId} Row: {itemAction.RowId} Type : {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////    PluginLog.Error($"Name: {test} : {item2.Name} ID : {item2.RowId} Row: {itemAction2.RowId} Type : {itemAction2.Type} EquipSlotCategory.Row : {item2.EquipSlotCategory.Row} IsUnique: {item2.IsUnique} IsUntradeable: {item2.IsUntradable}");
                ////    PluginLog.Error($"Name: {test} : {item3.Name} ID : {item3.RowId} Row: {itemAction3.RowId} Type : {itemAction3.Type} EquipSlotCategory.Row : {item3.EquipSlotCategory.Row} IsUnique: {item3.IsUnique} IsUntradeable: {item3.IsUntradable}");
                ////}

                ////switch (itemAction.Type)
                ////{
                ////    case 0xA49:  // Unlock Link (Emote, Hairstyle)
                ////        PluginLog.Information($"Emote/Hairstyle: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x355:  // Minions
                ////        PluginLog.Information($"Minions: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x3F5:  // Bardings
                ////        PluginLog.Information($"Bardings: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x52A:  // Mounts
                ////        PluginLog.Information($"Mounts: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0xD1D:  // Triple Triad Cards
                ////        PluginLog.Information($"Triple Triad Cards: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x4E76: // Ornaments
                ////        PluginLog.Information($"Ornaments: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x625F: // Orchestrion Rolls
                ////        PluginLog.Information($"Orchestrion Rolls: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    default:
                ////        continue;
                ////}
            }
        }

        private unsafe int GetItemCount(uint itemId)
        {
            //// Only check main inventories, don't include any special inventories
            var inventories = new List<InventoryType>
        {
            //// DefaultInventory
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4,
            //// Armory
            InventoryType.ArmoryBody,
            InventoryType.ArmoryEar,
            InventoryType.ArmoryFeets,
            InventoryType.ArmoryHands,
            InventoryType.ArmoryHead,
            InventoryType.ArmoryLegs,
            InventoryType.ArmoryMainHand,
            InventoryType.ArmoryNeck,
            InventoryType.ArmoryOffHand,
            InventoryType.ArmoryRings,
            InventoryType.ArmoryWaist,
            InventoryType.ArmoryWrist,
            //// EquipedGear
            InventoryType.EquippedItems,
        };
            return inventories.Sum(inventory => InventoryManager.Instance()->GetItemCountInContainer(itemId, inventory));
        }
    }
#endif
}
