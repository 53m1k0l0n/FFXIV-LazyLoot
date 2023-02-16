using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using LazyLoot.Attributes;
using LazyLoot.Config;
using LazyLoot.Ui;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LazyLoot.Plugin
{
    public class LazyLoot : IDalamudPlugin, IDisposable
    {
        public static DtrBarEntry DtrEntry;
        public static bool FulfEnabled;
        public ConfigUi ConfigUi;
        internal static Configuration config;
        internal static RollItemRaw rollItemRaw;
        private static IntPtr lootsAddr;
        private readonly LazyLootCommandManager<LazyLoot> commandManager;
        private readonly List<LootItem> items = new();
        private bool isRolling;
        private uint lastItem = 123456789;

        public LazyLoot(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service.Service>();
            lootsAddr = Service.Service.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(Service.Service.SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            config = Service.Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            config.Initialize(Service.Service.PluginInterface);
            ConfigUi = new ConfigUi(this);
            Service.Service.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigUi.IsOpen = true; };
            commandManager = new LazyLootCommandManager<LazyLoot>(this, Service.Service.PluginInterface);
            DtrEntry ??= Service.Service.DtrBar.Get("LazyLoot");

            Service.Service.Framework.Update += OnFrameworkUpdate;
        }

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        public string Name => "LazyLoot";

        public void Dispose()
        {
            PluginLog.Information(string.Format($">>Stop LazyLoot<<"));
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Command("/fulf")]
        [HelpMessage("En/Disable FULF")]
        [DoNotShowInHelp]
        public void EnDisableFluf(string command, string arguments)
        {
            var subArguments = arguments.Split(' ');

            if (subArguments[0] != "c")
            {
                FulfEnabled = !FulfEnabled;

                if (FulfEnabled)
                {
                    Service.Service.ToastGui.ShowQuest("FULF enabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    Service.Service.ChatGui.CheckMessageHandled += NoticeLoot;
                }
                else
                {
                    Service.Service.ToastGui.ShowQuest("FULF disabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    Service.Service.ChatGui.CheckMessageHandled -= NoticeLoot;
                }
            }

            if (subArguments.Length > 1)
            {
                SetRollOption(subArguments[1]);
            }
            else
            {
                SetRollOption(subArguments[0]);
            }
        }

        public unsafe long GetItemUnlockedAction(LootItem itemInfo)
        {
            return UIState.Instance()->IsItemActionUnlocked(ExdModule.GetItemRowById(itemInfo.ItemId));
        }

        [Command("/roll")]
        [HelpMessage("Roll for the loot according to the argument and the item's RollState.")]
        [DoNotShowInHelp]
        public async void RollCommand(string command, string arguments)
        {
            if (isRolling) return;
            if (FulfEnabled && !string.IsNullOrEmpty(command) && arguments != "passall") return;
            if (arguments.IsNullOrWhitespace() || (arguments != "need" && arguments != "needonly" && arguments != "greed" && arguments != "pass" && arguments != "passall")) return;

            isRolling = true;

            if (FulfEnabled && arguments != "passall")
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            items.AddRange(GetItems());

            if (items.All(x => x.Rolled) && arguments != "passall")
            {
                if (config.EnableToastMessage)
                {
                    Service.Service.ToastGui.ShowError(">>No new loot<<");
                }
                return;
            }

            int itemsNeed = 0;
            int itemsGreed = 0;
            int itemsPass = 0;

            var itemRolls = new Dictionary<int, RollOption>();

            try
            {
                for (int index = items.Count - 1; index >= 0; index--)
                {
                    var itemInfo = items[index];
                    if (itemInfo.ItemId is 0) continue;
                    LogBeforeRoll(index, itemInfo);
                    if (!items[index].Rolled || arguments == "passall")
                    {
                        var itemData = Service.Service.Data.GetExcelSheet<Item>()!.GetRow(itemInfo.ItemId);
                        if (itemData is null) continue;
                        PluginLog.LogInformation(string.Format($"Item Data : {itemData.Name} : Row {itemData.ItemAction.Row} : IsUnique = {itemData.IsUnique} : IsUntradable = {itemData.IsUntradable} : Unlocked = {GetItemUnlockedAction(itemInfo)}"));
                        switch (itemData)
                        {
                            // Item is non unique
                            case { IsUnique: false }:
                            // [OR] Item is unique, and isn't consumable, just check quantity. If zero means we dont have it in our inventory.
                            case { IsUnique: true, ItemAction.Row: 0 } when GetItemCount(itemInfo.ItemId) == 0:
                            // [OR] Item has a unlock action (Minions, cards, orchestrations, mounts, etc), 2 means item has not been unlocked
                            case { ItemAction.Row: not 0 } when GetItemUnlockedAction(itemInfo) is 2:
                                itemRolls.Add(index, RollStateToOption(items[index].RollState, arguments));
                                break;

                            default:
                                itemRolls.Add(index, RollOption.Pass);
                                break;
                        }
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

        [Command("/lazy")]
        [HelpMessage("Open Lazy Loot config.")]
        public void OpenConfig(string command, string arguments)
        {
            ConfigUi.IsOpen = !ConfigUi.IsOpen;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            commandManager.Dispose();
            DtrEntry.Remove();

            if (FulfEnabled)
            {
                Service.Service.ChatGui.CheckMessageHandled -= NoticeLoot;
            }

            Service.Service.Framework.Update -= OnFrameworkUpdate;
            Service.Service.PluginInterface.SavePluginConfig(config);
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

            if (config.EnableChatLogMessage)
            {
                Service.Service.ChatGui.Print(seString);
            }

            if (config.EnableToastMessage)
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

        private void LogBeforeRoll(int index, LootItem lootItem)
        {
            PluginLog.LogInformation(string.Format($"Before : [{index}] {lootItem.ItemId} Id: {lootItem.ObjectId:X} rollState: {lootItem.RollState} rollOption: {lootItem.RolledState} rolled: {lootItem.Rolled}"));
        }

        private void NoticeLoot(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
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
                RollCommand(string.Empty, SetFulfArguments());
                Service.Service.PluginInterface.UiBuilder.AddNotification(">>New Loot<<", "Lazy Loot", NotificationType.Info);
            }
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (FulfEnabled)
            {
                DtrEntry.Text = "LL-FULF";

                if (config.EnableNeedRoll)
                {
                    DtrEntry.Text += " - Need";
                }
                else if (config.EnableNeedOnlyRoll)
                {
                    DtrEntry.Text += " - Need Only";
                }
                else if (config.EnableGreedRoll)
                {
                    DtrEntry.Text += " - Greed Only";
                }
                else if (config.EnablePassRoll)
                {
                    DtrEntry.Text += " - Pass";
                }
                DtrEntry.Shown = true;
            }
            else
            {
                DtrEntry.Shown = false;
            }
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

        private async Task RollItemAsync(RollOption option, int index)
        {
            rollItemRaw(lootsAddr, option, (uint)index);

            if (config.EnableRollDelay)
            {
                await Task.Delay(TimeSpan.FromSeconds(config.RollDelayInSeconds).Add(TimeSpan.FromMilliseconds(new Random().Next(251))));
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
            if (config.EnableNeedRoll)
            {
                return "need";
            }
            else if (config.EnableNeedOnlyRoll)
            {
                return "needonly";
            }
            else if (config.EnableGreedRoll)
            {
                return "greed";
            }
            else
            {
                return "pass";
            }
        }

        private void SetRollOption(string subArgument)
        {
            switch (subArgument)
            {
                case "need":
                    config.EnableNeedRoll = true;
                    config.EnableNeedOnlyRoll = false;
                    config.EnableGreedRoll = false;
                    config.EnablePassRoll = false;
                    break;

                case "needonly":
                    config.EnableNeedRoll = false;
                    config.EnableNeedOnlyRoll = true;
                    config.EnableGreedRoll = false;
                    config.EnablePassRoll = false;
                    break;

                case "greed":
                    config.EnableNeedRoll = false;
                    config.EnableNeedOnlyRoll = false;
                    config.EnableGreedRoll = true;
                    config.EnablePassRoll = false;
                    break;

                case "pass":
                    config.EnableNeedRoll = false;
                    config.EnableNeedOnlyRoll = false;
                    config.EnableGreedRoll = false;
                    config.EnablePassRoll = true;
                    break;
            }
        }

        private void ToastOutput(SeString seString)
        {
            if (config.EnableNormalToast)
            {
                Service.Service.ToastGui.ShowNormal(seString);
            }
            else if (config.EnableQuestToast)
            {
                Service.Service.ToastGui.ShowQuest(seString);
            }
            else if (config.EnableErrorToast)
            {
                Service.Service.ToastGui.ShowError(seString);
            }
        }
    }
}