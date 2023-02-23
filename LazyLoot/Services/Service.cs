using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace LazyLoot.Services
{
    public class Service
    {
        [PluginService] public static ChatGui ChatGui { get; private set; } = null!;

        [PluginService] public static ClientState ClientState { get; private set; } = null!;

        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;

        [PluginService] public static DataManager Data { get; private set; } = null!;

        [PluginService] public static DtrBar DtrBar { get; private set; } = null!;

        [PluginService] public static Framework Framework { get; private set; } = null!;

        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        [PluginService] public static SigScanner SigScanner { get; private set; } = null!;

        [PluginService] public static ToastGui ToastGui { get; private set; } = null!;

        [PluginService] public static Condition Condition { get; private set; } = null!;
    }
}