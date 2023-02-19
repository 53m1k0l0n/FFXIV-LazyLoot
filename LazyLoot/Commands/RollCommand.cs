using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using LazyLoot.Attributes;
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
        private readonly uint lastItem = 123456789;
        private bool isRolling;

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        public override void Initialize()
        {
            lootsAddr = Service.Service.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(Service.Service.SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            base.Initialize();
        }

        public void NoticeLoot(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (isRolling) return;
            if ((ushort)type != 2105) return;
            if (message.TextValue == Service.Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "Bitte um das Beutegut würfeln.",
                ClientLanguage.French => "Veuillez lancer les dés pour le butin.",
                ClientLanguage.Japanese => "ロットを行ってください。",
                _ => "Cast your lot."
            })
            {
                Service.Service.PluginInterface.UiBuilder.AddNotification(">>New Loot<<", "Lazy Loot", NotificationType.Info);
                Roll(string.Empty, SetFulfArguments());
            }
        }

        [Command("/roll", "Roll for the loot according to the argument and the item's RollState. /roll need | needonly | greed | pass or passall")]
        public async void Roll(string command, string arguments)
        {
            if (isRolling) return;
            if (Plugin.LazyLoot.FulfEnabled && !string.IsNullOrEmpty(command) && arguments != "passall") return;
            if (arguments.IsNullOrWhitespace() || (arguments != "need" && arguments != "needonly" && arguments != "greed" && arguments != "pass" && arguments != "passall")) return;

            isRolling = true;

            if (Plugin.LazyLoot.FulfEnabled && arguments != "passall")
            {
                await Task.Delay(TimeSpan.FromSeconds(Plugin.LazyLoot.config.FulfDelay));
            }

            items.AddRange(GetItems());

            if (items.All(x => x.Rolled) && arguments != "passall")
            {
                if (Plugin.LazyLoot.config.EnableToastMessage)
                {
                    Service.Service.ToastGui.ShowError(">>No new loot<<");
                }
                isRolling = false;
                return;
            }

            int itemsNeed = 0;
            int itemsGreed = 0;
            int itemsPass = 0;

            try
            {
                var itemRolls = new Dictionary<int, RollOption>();

                for (int index = items.Count - 1; index >= 0; index--)
                {
                    var itemInfo = items[index];
                    if (itemInfo.ItemId is 0) continue;
                    LogBeforeRoll(index, itemInfo);
                    if (!items[index].Rolled || arguments == "passall")
                    {
                        var itemData = Service.Service.Data.GetExcelSheet<Item>()!.GetRow(itemInfo.ItemId);
                        if (itemData is null) continue;
                        PluginLog.LogInformation(string.Format($"Item Data : {itemData.Name} : Row {itemData.ItemAction.Row} : ILvl = {itemData.LevelItem.Row} : IsUnique = {itemData.IsUnique} : IsUntradable = {itemData.IsUntradable} : Unlocked = {GetItemUnlockedAction(itemInfo)}"));
                        var rollItem = RollCheck(arguments, index, itemInfo, itemData);
                        itemRolls.Add(rollItem.Index, rollItem.RollOption);
                    }
                }

                // Roll items
                foreach (KeyValuePair<int, RollOption> entry in itemRolls)
                {
                    await RollItemAsync(entry.Value, entry.Key);
                    switch (entry)
                    {
                        case { Value: RollOption.Need }:
                            itemsNeed++;
                            break; ;
                        case { Value: RollOption.Greed }:
                            itemsGreed++;
                            break; ;
                        case { Value: RollOption.Pass }:
                            itemsPass++;
                            break; ;
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
                items.Clear();
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
                Service.Service.ChatGui.Print(seString);
            }

            if (Plugin.LazyLoot.config.EnableToastMessage)
            {
                ToastOutput(seString);
            }
        }

        private LootItem GetItem(int index)
        {
            try
            {
                return ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.Valid).ToList()[index];
            }
            catch
            {
                return new LootItem() { ItemId = lastItem, RolledState = RollOption.NotAvailable };
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

        private void LogBeforeRoll(int index, LootItem lootItem)
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

        private (int Index, RollOption RollOption) RollCheck(string arguments, int index, LootItem itemInfo, Item? itemData)
        {
            switch (itemData)
            {
                // First checking FilterRules
                // Item is already unlocked
                case { ItemAction.Row: >= 0 } when GetItemUnlockedAction(itemInfo) is 1 && Plugin.LazyLoot.config.RestrictionIgnoreItemUnlocked:
                // [OR] Item level doesnt match
                case { EquipSlotCategory.Row: not 0 } when itemData.LevelItem.Row <= Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelowValue && Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelow:
                    return (Index: index, RollOption: RollOption.Pass);

                // If non of the FilterRules are active.
                // Item is non unique
                case { IsUnique: false }:
                // [OR] Item is unique, and isn't consumable, just check quantity. If zero means we dont have it in our inventory.
                case { IsUnique: true, ItemAction.Row: 0 } when GetItemCount(itemInfo.ItemId) == 0:
                // [OR] Item has a unlock action (Minions, cards, orchestrations, mounts, etc),
                // 2 means item has not been unlocked and 4 well i don't know yet, but for now we need it, for items which are UnIque and not ItemAction.Row 0.
                case { ItemAction.Row: not 0 } when GetItemUnlockedAction(itemInfo) is not 1:
                    return (Index: index, RollOption: RollStateToOption(items[index].RollState, arguments));

                default:
                    return (Index: index, RollOption: RollOption.Pass);
            }
        }

        private async Task RollItemAsync(RollOption option, int index)
        {
            rollItemRaw(lootsAddr, option, (uint)index);

            if (Plugin.LazyLoot.config.EnableRollDelay)
            {
                await Task.Delay(TimeSpan.FromSeconds(Plugin.LazyLoot.config.RollDelayInSeconds).Add(TimeSpan.FromMilliseconds(new Random().Next(251))));
            }

            LootItem lootItem = GetItem(index);
            PluginLog.LogInformation(string.Format($"After : {option} [{index}] {lootItem.ItemId} Id: {lootItem.ObjectId:X} rollState: {lootItem.RollState} rollOption: {lootItem.RolledState} rolled: {lootItem.Rolled}"));
        }

        private RollOption RollStateToOption(RollState rollState, string arguments)
        {
            return rollState switch
            {
                RollState.UpToNeed when arguments == "need" || arguments == "needonly" => RollOption.Need,
                RollState.UpToGreed when arguments == "greed" || arguments == "need" => RollOption.Greed,
                _ => RollOption.Pass,
            };
        }

        private string SetFulfArguments()
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
                Service.Service.ToastGui.ShowNormal(seString);
            }
            else if (Plugin.LazyLoot.config.EnableQuestToast)
            {
                Service.Service.ToastGui.ShowQuest(seString);
            }
            else if (Plugin.LazyLoot.config.EnableErrorToast)
            {
                Service.Service.ToastGui.ShowError(seString);
            }
        }
    }
}