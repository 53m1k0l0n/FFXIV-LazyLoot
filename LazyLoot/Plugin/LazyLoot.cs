using Dalamud;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.IoC;
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
        public static bool flufEnabled;
        public ConfigUi configUi;
        internal static Configuration config;
        internal static RollItemRaw rollItemRaw;
        private static IntPtr lootsAddr;
        private readonly LazyLootCommandManager<LazyLoot> commandManager;
        private readonly List<LootItem> items = new();
        private readonly OverlayUi overlay;
        private uint lastItem = 123456789;
        private bool rolling;

        public LazyLoot(DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;
            lootsAddr = SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            config.Initialize(PluginInterface);
            configUi = new ConfigUi(this);
            overlay = new OverlayUi();
            configUi.windowSystem.AddWindow(overlay);
            PluginInterface.UiBuilder.OpenConfigUi += delegate { configUi.IsOpen = true; };
            commandManager = new LazyLootCommandManager<LazyLoot>(this, PluginInterface);
        }

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);

        [PluginService]
        public static ChatGui ChatGui { get; set; }

        [PluginService]
        public static ClientState ClientState { get; private set; }

        [PluginService]
        public static CommandManager CommandManager { get; set; }

        [PluginService]
        public static DataManager Data { get; private set; }

        [PluginService]
        public static DalamudPluginInterface PluginInterface { get; set; }

        [PluginService]
        public static SigScanner SigScanner { get; set; }

        [PluginService]
        public static ToastGui ToastGui { get; private set; }

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
                flufEnabled = !flufEnabled;

                if (flufEnabled)
                {
                    ToastGui.ShowQuest("FULF enabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    ChatGui.CheckMessageHandled += NoticeLoot;
                }
                else
                {
                    ToastGui.ShowQuest("FULF disabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    ChatGui.CheckMessageHandled -= NoticeLoot;
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

        [Command("/fulf?")]
        [HelpMessage("FULF status")]
        [DoNotShowInHelp]
        public void CheckFlufStatus(string command, string arguments)
        {
            if (flufEnabled)
            {
                if (LazyLoot.config.EnableNeedRoll)
                {
                    ToastGui.ShowQuest("FULF is enabled with Need");
                }
                else if (LazyLoot.config.EnableNeedOnlyRoll)
                {
                    ToastGui.ShowQuest("FULF is enabled with Needonly");
                }
                else if (LazyLoot.config.EnableGreedRoll)
                {
                    ToastGui.ShowQuest("FULF is enabled with greed");
                }
                else
                {
                    ToastGui.ShowQuest("FULF is enabled");
                }
            }
            else
            {
                ToastGui.ShowQuest("FULF is disabled");
            }
        }

        [Command("/roll")]
        [HelpMessage("Roll need for everything. If impossible roll greed or pass if greed is impossible.")]
        [DoNotShowInHelp]
        public async void NeedCommand(string command, string arguments)
        {
            if (arguments.IsNullOrWhitespace() || arguments != "need" && arguments != "needonly" && arguments != "greed" && arguments != "pass" && arguments != "passall") return;

            items.AddRange(GetItems());
            if (items.All(x => x.Rolled))
            {
                if (config.EnableToastMessage)
                {
                    ToastGui.ShowError(">>No new loot<<");
                }
                return;
            }

            int itemsNeed = 0;
            int itemsGreed = 0;
            int itemsPass = 0;

            var itemRolls = new Dictionary<int, RollOption>();

            for (int index = items.Count - 1; index >= 0; index--)
            {
                var itemInfo = items[index];
                LogBeforeRoll(index, itemInfo);
                if (!items[index].Rolled || arguments == "passall")
                {
                    var itemData = Data.GetExcelSheet<Item>()!.GetRow(itemInfo.ItemId);
                    switch (itemData)
                    {
                        // Item is non unique
                        case { IsUnique: false }:
                        // [OR] Item is unique, and isn't consumable, just check quantity. If zero means we dont have it in our inventory.
                        case { IsUnique: true, ItemAction.Row: 0 } when GetItemCount(itemInfo.ItemId) == 0:
                        // [OR] Item has a unlock action (Minions, cards, orchestrations, mounts, etc), 0 means item has not been unlocked
                        case { ItemAction.Row: not 0 } when GetItemUnlockedAction(itemInfo) is 0:
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
            items.Clear();
            rolling = false;
        }

        [Command("/lazy")]
        [HelpMessage("Open Lazy Loot config.")]
        public void OpenConfig(string command, string arguments)
        {
            configUi.IsOpen = !configUi.IsOpen;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            commandManager.Dispose();

            if (flufEnabled)
            {
                ChatGui.CheckMessageHandled -= NoticeLoot;
            }

            PluginInterface.SavePluginConfig(config);
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

            if (LazyLoot.config.EnableChatLogMessage)
            {
                ChatGui.Print(seString);
            }

            ToastOutput(seString);
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
            // Only check main inventories, don't include any special inventories
            var inventories = new List<InventoryType>
        {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4,
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
            PluginLog.Information(string.Format($"Before : [{index}] {lootItem.ItemId} Id: {lootItem.ObjectId:X} rollState: {lootItem.RollState} rollOption: {lootItem.RolledState} rolled: {lootItem.Rolled}"));
        }

        private void NoticeLoot(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (rolling) return;
            if ((ushort)type != 2105) return;
            if (message.TextValue == ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "Bitte um das Beutegut würfeln.",
                ClientLanguage.French => "Veuillez lancer les dés pour le butin.",
                ClientLanguage.Japanese => "ロットを行ってください。",
                _ => "Cast your lot."
            })
            {
                LazyLoot.PluginInterface.UiBuilder.AddNotification(">>New Loot<<", "Lazy Loot", NotificationType.Info);
                ////NeedCommand(string.Empty, string.Empty);
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

            if (LazyLoot.config.EnableRollDelay)
            {
                await Task.Delay(TimeSpan.FromSeconds(LazyLoot.config.RollDelayInSeconds).Add(TimeSpan.FromMilliseconds(new Random().Next(251))));
            }

            LootItem lootItem = GetItem(index);
            PluginLog.Information(string.Format($"After : {option} [{index}] {lootItem.ItemId} Id: {lootItem.ObjectId:X} rollState: {lootItem.RollState} rollOption: {lootItem.RolledState} rolled: {lootItem.Rolled}"));
        }

        private RollOption RollStateToOption(RollState rollState, string arguments)
        {
            return rollState switch
            {
                RollState.UpToNeed when arguments == "need" || arguments == "needonly" => RollOption.Need,
                RollState.UpToGreed when arguments != "pass" || arguments != "passall" => RollOption.Greed,
                _ => RollOption.Pass,
            };
        }

        private void SetRollOption(string subArgument)
        {
            switch (subArgument)
            {
                case "need":
                    LazyLoot.config.EnableNeedRoll = true;
                    LazyLoot.config.EnableNeedOnlyRoll = false;
                    LazyLoot.config.EnableGreedRoll = false;
                    break;

                case "needonly":
                    LazyLoot.config.EnableNeedRoll = false;
                    LazyLoot.config.EnableNeedOnlyRoll = true;
                    LazyLoot.config.EnableGreedRoll = false;
                    break;

                case "greed":
                    LazyLoot.config.EnableNeedRoll = false;
                    LazyLoot.config.EnableNeedOnlyRoll = false;
                    LazyLoot.config.EnableGreedRoll = true;
                    break;
            }
        }

        private void ToastOutput(SeString seString)
        {
            if (LazyLoot.config.EnableToastMessage && LazyLoot.config.EnableNormalToast)
            {
                ToastGui.ShowNormal(seString);
            }

            if (LazyLoot.config.EnableToastMessage && LazyLoot.config.EnableQuestToast)
            {
                ToastGui.ShowQuest(seString);
            }

            if (LazyLoot.config.EnableToastMessage && LazyLoot.config.EnableErrorToast)
            {
                ToastGui.ShowError(seString);
            }
        }
    }
}