﻿using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using LootMaster.Attributes;
using LootMaster.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LootMaster.Plugin
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        internal static Configuration config;
        private static IntPtr lootsAddr;
        internal static RollItemRaw rollItemRaw;
        private readonly PluginCommandManager<Plugin> commandManager;
        private readonly PluginUI ui;

        [PluginService]
        public static CommandManager CommandManager { get; set; }

        [PluginService]
        public static DalamudPluginInterface PluginInterface { get; set; }

        [PluginService]
        public static SigScanner SigScanner { get; set; }

        [PluginService]
        public static ChatGui ChatGui { get; set; }

        [PluginService]
        public static ToastGui ToastGui { get; private set; }

        public static List<LootItem> LootItems => ReadArray<LootItem>(lootsAddr + 16, 16).Where(i => i.Valid).ToList();

        public string Name => "LazyLoot";

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            lootsAddr = SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
            rollItemRaw = Marshal.GetDelegateForFunctionPointer<RollItemRaw>(SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            config.Initialize(PluginInterface);
            ui = new PluginUI();
            PluginInterface.UiBuilder.Draw += new Action(ui.Draw);
            PluginInterface.UiBuilder.OpenConfigUi += () =>
           {
               PluginUI ui = this.ui;
               ui.IsVisible = !ui.IsVisible;
           };
            commandManager = new PluginCommandManager<Plugin>(this, PluginInterface);
        }

        private void RollItem(RollOption option, int index)
        {
            LootItem lootItem = LootItems[index];
            rollItemRaw(lootsAddr, option, (uint)index);
            PluginLog.Information(string.Format("{0} [{1}] {2} Id: {3:X} rollState: {4} rollOption: {5}", option, index, lootItem.ItemId, lootItem.ObjectId, lootItem.RollState, lootItem.RolledState), Array.Empty<object>());
        }

        [Command("/need")]
        [HelpMessage("Roll need for everything. If impossible roll greed.")]
        public void NeedCommand(string command, string args)
        {
            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (!LootItems[index].Rolled)
                {
                    if (LootItems[index].RollState == RollState.UpToNeed)
                    {
                        RollItem(RollOption.Need, index);
                        ++num1;
                    }
                    else if (!(LootItems[index].RollState == RollState.UpToNeed) && !(LootItems[index].RollState == RollState.UpToGreed))
                    {
                        RollItem(RollOption.Pass, index);
                        ++num3;
                    }
                    else if (!LootItems[index].Rolled)
                    {
                        RollItem(RollOption.Greed, index);
                        ++num2;
                    }
                }
            }

            ChatGui chatGui = ChatGui;
            List<Payload> payloadList = new()
            {
                new TextPayload("Need "), new UIForegroundPayload(575), new TextPayload(num1.ToString()),
                new UIForegroundPayload(0), new TextPayload(" item" + (num1 > 1 ? "s" : "") + ", greed "),
                new UIForegroundPayload(575), new TextPayload(num2.ToString()), new UIForegroundPayload(0),
                new TextPayload(" item" + (num2 > 1 ? "s" : "") + ", pass "),
                new UIForegroundPayload(575),
                new TextPayload(num2.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num3 > 1 ? "s" : "") + ".")
            };
            SeString seString = new(payloadList);

            if (config.EnableChatLogMessage)
            {
                chatGui.Print(seString);
            }

            if (config.EnableToastMessage)
            {
                ToastGui.ShowNormal(seString);
            }
        }

        [Command("/needonly")]
        [HelpMessage("Roll need for everything. If impossible roll pass")]
        public void NeedOnlyCommand(string command, string args)
        {
            int num1 = 0;
            int num2 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (!LootItems[index].Rolled)
                {
                    if (LootItems[index].RollState == RollState.UpToNeed)
                    {
                        RollItem(RollOption.Need, index);
                        ++num1;
                    }
                    else
                    {
                        RollItem(RollOption.Pass, index);
                        ++num2;
                    }
                }
            }

            ChatGui chatGui = ChatGui;
            List<Payload> payloadList = new()
            {
                new TextPayload("Need Only"),
                new UIForegroundPayload(575),
                new TextPayload(num1.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num1 > 1 ? "s" : "") + ", pass "),
                new UIForegroundPayload(575),
                new TextPayload(num2.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num2 > 1 ? "s" : "") + ".")
            };
            SeString seString = new(payloadList);

            if(config.EnableChatLogMessage)
            {
                chatGui.Print(seString);
            }

            if (config.EnableToastMessage)
            {
                ToastGui.ShowNormal(seString);
            }
        }

        [Command("/greed")]
        [HelpMessage("Greed on all items.")]
        public void GreedCommand(string command, string args)
        {
            int num = 0;
            int num2 = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RollState == RollState.UpToGreed)
                {
                    RollItem(RollOption.Greed, index);
                    ++num;
                }
            }

            ChatGui chatGui = ChatGui;
            List<Payload> payloadList = new()
            {
                new TextPayload("Greed "),
                new UIForegroundPayload(575),
                new TextPayload(num.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num > 1 ? "s" : "") + ", pass "),
                new UIForegroundPayload(575),
                new TextPayload(num2.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num2 > 1 ? "s" : "") + ".")
            };
            SeString seString = new(payloadList);

            if (config.EnableChatLogMessage)
            {
                chatGui.Print(seString);
            }

            if (config.EnableToastMessage)
            {
                ToastGui.ShowNormal(seString);
            }
        }

        [Command("/pass")]
        [HelpMessage("Pass on things you haven't rolled for yet.")]
        public void PassCommand(string command, string args)
        {
            int num = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (!LootItems[index].Rolled)
                {
                    RollItem(RollOption.Pass, index);
                    ++num;
                }
            }
            ChatGui chatGui = ChatGui;
            List<Payload> payloadList = new()
            {
                new TextPayload("Pass "),
                new UIForegroundPayload(575),
                new TextPayload(num.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num > 1 ? "s" : "") + ".")
            };
            SeString seString = new(payloadList);

            if (config.EnableChatLogMessage)
            {
                chatGui.Print(seString);
            }

            if (config.EnableToastMessage)
            {
                ToastGui.ShowNormal(seString);
            }
        }

        [Command("/passall")]
        [HelpMessage("Passes on all, even if you rolled on them previously.")]
        public void PassAllCommand(string command, string args)
        {
            int num = 0;
            for (int index = 0; index < LootItems.Count; ++index)
            {
                if (LootItems[index].RolledState != RollOption.Pass)
                {
                    RollItem(RollOption.Pass, index);
                    ++num;
                }
            }

            ChatGui chatGui = ChatGui;
            List<Payload> payloadList = new()
            {
                new TextPayload("Pass all "),
                new UIForegroundPayload(575),
                new TextPayload(num.ToString()),
                new UIForegroundPayload(0),
                new TextPayload(" item" + (num > 1 ? "s" : "") + ".")
            };
            SeString seString = new(payloadList);

            if (config.EnableChatLogMessage) 
            { 
                chatGui.Print(seString);
            }

            if (config.EnableToastMessage)
            {
                ToastGui.ShowNormal(seString);
            }
        }

        public static T[] ReadArray<T>(IntPtr unmanagedArray, int length) where T : struct
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            commandManager.Dispose();
            PluginInterface.SavePluginConfig(config);
            PluginInterface.UiBuilder.Draw -= new Action(ui.Draw);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);
    }
}
