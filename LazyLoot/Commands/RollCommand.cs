using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using LazyLoot.Attributes;
using LazyLoot.Services;
using LazyLoot.Util;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LazyLoot.Commands
{
    public class RollCommand : BaseCommand
    {
        internal static IntPtr lootsAddr;
        internal static RollItemRaw rollItemRaw;
        private readonly List<LootItem> items = new();
        private uint lastItem = 123456789;
        public bool isRolling;

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        public override void Initialize()
        {
            lootsAddr = Service.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(Service.SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            base.Initialize();
        }

        [Command("/rolling", "Roll for the loot according to the argument and the item's RollState. /rolling need | needonly | greed | pass or passall")]
        public async void Roll(string command, string arguments)
        {
            if (isRolling) return;
            if (Plugin.LazyLoot.FulfEnabled && !string.IsNullOrEmpty(command) && arguments != "passall") return;
            if (arguments.IsNullOrWhitespace() || (arguments != "need" && arguments != "needonly" && arguments != "greed" && arguments != "pass" && arguments != "passall")) return;

            isRolling = true;

            if (Plugin.LazyLoot.FulfEnabled)
            {
                await Task.Delay(new Random().Next(1501));
            }

            if (GetLastNotRolledItem().LootItem is null  && arguments != "passall")
            {
                if (Plugin.LazyLoot.config.EnableToastMessage)
                {
                    Service.ToastGui.ShowError(">>No new loot<<");
                }
                isRolling = false;
                return;
            }

            int itemsNeed = 0;
            int itemsGreed = 0;
            int itemsPass = 0;

            try
            {
                while(GetLastNotRolledItem().LootItem is not null )
                {
                    var item = GetLastNotRolledItem();
                    if (item.LootItem is null) break;
                    LootItem itemInfo = (LootItem)item.LootItem;
                    if (itemInfo.ItemId is 0) continue;
                    lastItem = itemInfo.ItemId;
                    LogBeforeRoll(item.Index, itemInfo);
                        var itemData = Service.Data.GetExcelSheet<Item>()!.GetRow(itemInfo.ItemId);
                        if (itemData is null) continue;
                        PluginLog.LogInformation(string.Format($"Item Data : {itemData.Name} : Row {itemData.ItemAction.Row} : ILvl = {itemData.LevelItem.Row} :  Type = {itemData.ItemAction.Value.Type} : IsUnique = {itemData.IsUnique} : IsUntradable = {itemData.IsUntradable} : Unlocked = {GetItemUnlockedAction(itemInfo)}"));
                        var rollItem = RollCheck(arguments, item.Index, itemInfo, itemData);

                        if (Service.Condition[ConditionFlag.BoundByDuty])
                        {

                            await RollItemAsync(rollItem.RollOption, rollItem.Index);

                                switch (rollItem)
                                {
                                    case { RollOption: RollOption.Need }:
                                        itemsNeed++;
                                        break; ;
                                    case { RollOption: RollOption.Greed }:
                                        itemsGreed++;
                                        break; ;
                                    case { RollOption: RollOption.Pass }:
                                        itemsPass++;
                                        break; ;
                                }
                        }
                        else
                        {
                            break;
                        }
                }

                ChatOutput(itemsNeed, itemsGreed, itemsPass);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Something went really bad. Please contact the author!");
            }
            finally
            {
                isRolling = false;
            }
        }

        private void ChatOutput(int num1, int num2, int num3)
        {
            List<Payload> payloadList = new()
            {
                new TextPayload("Need "),
                new UIForegroundPayload(575),
                new TextPayload(num1.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num1 > 1 ? "s" : "") + ", greed "),
                new UIForegroundPayload(575),
                new TextPayload(num2.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num2 > 1 ? "s" : "") + ", pass "),
                new UIForegroundPayload(575),
                new TextPayload(num3.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num3 > 1 ? "s" : "") + ".")
            };

            SeString seString = new(payloadList);

            if (Plugin.LazyLoot.config.EnableChatLogMessage)
            {
                Service.ChatGui.Print(seString);
            }

            if (Plugin.LazyLoot.config.EnableToastMessage)
            {
                ToastOutput(seString);
            }
        }

        private (uint Index , LootItem? LootItem) GetLastNotRolledItem()
        {
            items.Clear();
            items.AddRange(GetItems());

            for (int index = items.Count - 1; index >= 0; index--)
            {
                if (!items[index].Rolled)
                {
                    return ((uint)index, items[index]);
                }
            }

            return (0, null);
        }

        private LootItem GetItem(int index)
        {
            try
            {
                return ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.Valid).ToList()[index];
            }
            catch
            {
                return new LootItem() { ItemId = lastItem, RolledState = RollOption.Rolled };
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

        private List<LootItem> GetItems()
        {
            return ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.Valid).ToList();
        }

        private unsafe long GetItemUnlockedAction(LootItem itemInfo)
        {
            return UIState.Instance()->IsItemActionUnlocked(ExdModule.GetItemRowById(itemInfo.ItemId));
        }

        private void LogBeforeRoll(uint index, LootItem lootItem)
        {
            PluginLog.LogInformation(string.Format($"Before : [{index}] {lootItem.ItemId} Id: {lootItem.ObjectId:X} rollState: {lootItem.RollState} rollOption: {lootItem.RolledState} rolled: {lootItem.Rolled}"));
        }

        private T[] ReadArray<T>(IntPtr unmanagedArray, int length) where T : struct
        {
            int num = Marshal.SizeOf(typeof(T));
            T[] objArray = new T[length];
            for (int index = 0; index < length; ++index)
            {
                IntPtr ptr = new(unmanagedArray.ToInt64() + index * num);
                objArray[index] = Marshal.PtrToStructure<T>(ptr);
            }
            return objArray;
        }

        private (uint Index, RollOption RollOption) RollCheck(string arguments, uint index, LootItem itemInfo, Item? itemData)
        {
            switch (itemData)
            {
                // First checking FilterRules
                // Item is already unlocked ALL Items
                case { ItemAction.Row: not 0 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreItemUnlocked:
                // Mounts 1332
                case { ItemAction.Value.Type: 1322 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreMounts:
                // Minions 853
                case { ItemAction.Value.Type: 853 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreMinions:
                // Bardings 1013
                case { ItemAction.Value.Type: 1013 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreBardings:
                // Triple Triad Cards 3357
                case { ItemAction.Value.Type: 3357 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreTripleTriadCards:
                // Emote/Hairstyle 2633
                case { ItemAction.Value.Type: 2633 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreEmoteHairstyle:
                // OrchestrionRolls 25183
                case { ItemAction.Value.Type: 25183 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreOrchestrionRolls:
                // FadedCopy 0
                case { ItemAction.Value.Type: 0 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreFadedCopy && itemData.Name.RawString.StartsWith("Faded Copy "):
                // [OR] Item level doesnt match
                case { EquipSlotCategory.Row: not 0 } when itemData.LevelItem.Row <= Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelowValue && Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelow:
                    // [OR] Can't wear it with the actual class
                    ////case { ClassJobCategory.IsValueCreated: true } when !IsClassValid(itemData)  :
                    return (Index: index, RollOption: RollOption.Pass);

                // If non of the FilterRules are active.
                // Item is unique, and isn't consumable, just check quantity. If zero means we dont have it in our inventory.
                case { IsUnique: true, ItemAction.Row: 0 } when GetItemCount(itemInfo.ItemId) == 0:
                // [OR] Item has a unlock action (Minions, cards, orchestrations, mounts, etc),
                // 2 means item has not been unlocked and 4 well i don't know yet, but for now we need it, for items which are UnIque and not ItemAction.Row 0.
                case { ItemAction.Row: not 0 } when GetItemUnlockedAction(itemInfo) is not 1:
                // [OR] Item is non unique
                case { IsUnique: false }:
                    return (Index: index, RollOption: RollStateToOption(itemInfo.RollState, arguments));

                default:
                    return (Index: index, RollOption: RollOption.Pass);
            }
        }

        private bool IsClassValid(Item itemData)
        {
            if (itemData.ClassJobCategory.Value.ACN && Service.ClientState.LocalPlayer.ClassJob.GameData.ClassJobCategory.Value.ACN)
            {
                return true;
            }
            else if (itemData.ClassJobCategory.Value.ADV && Service.ClientState.LocalPlayer.ClassJob.GameData.ClassJobCategory.Value.ADV)
            {
                return true;
            }
            else if (itemData.ClassJobCategory.Value.ALC && Service.ClientState.LocalPlayer.ClassJob.GameData.ClassJobCategory.Value.ALC)
            {
                return true;
            }
            else 
            { 
                return false;
            }
        }

        private async Task RollItemAsync(RollOption option, uint index)
        {
            PluginLog.LogInformation(string.Format("Rolling"));

            rollItemRaw(lootsAddr, option, index);

                if (Plugin.LazyLoot.config.EnableRollDelay)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Plugin.LazyLoot.config.RollDelayInSeconds).Add(TimeSpan.FromMilliseconds(new Random().Next(251))));
                }

                LootItem lootItem = GetItem((int)index);
                PluginLog.LogInformation(string.Format($"After : {option} [{index}] {lootItem.ItemId} Id: {lootItem.ObjectId:X} rollState: {lootItem.RollState} rollOption: {lootItem.RolledState} rolled: {lootItem.Rolled}"));

        }

        private RollOption RollStateToOption(RollState rollState, string arguments)
        {
            return rollState switch
            {
                RollState.UpToNeed when arguments == "need" || arguments == "needonly" => RollOption.Need,
                RollState.UpToGreed or RollState.UpToNeed when arguments == "greed" || arguments == "need" => RollOption.Greed,
                _ => RollOption.Pass,
            };
        }

        public string SetFulfArguments()
        {
            if (Plugin.LazyLoot.config.EnableNeedRoll)
            {
                return "need";
            }
            else if (Plugin.LazyLoot.config.EnableNeedOnlyRoll)
            {
                return "needonly";
            }
            else if (Plugin.LazyLoot.config.EnableGreedRoll)
            {
                return "greed";
            }
            else
            {
                return "pass";
            }
        }

        private void ToastOutput(SeString seString)
        {
            if (Plugin.LazyLoot.config.EnableNormalToast)
            {
                Service.ToastGui.ShowNormal(seString);
            }
            else if (Plugin.LazyLoot.config.EnableQuestToast)
            {
                Service.ToastGui.ShowQuest(seString);
            }
            else if (Plugin.LazyLoot.config.EnableErrorToast)
            {
                Service.ToastGui.ShowError(seString);
            }
        }
    }
}