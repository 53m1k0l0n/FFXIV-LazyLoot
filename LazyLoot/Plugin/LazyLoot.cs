using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Logging;
using Dalamud.Plugin;
using LazyLoot.Commands;
using LazyLoot.Config;
using LazyLoot.Ui;
using LazyLoot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LazyLoot.Plugin
{
    public class LazyLoot : IDalamudPlugin, IDisposable
    {
        internal static Configuration config;
        internal static ConfigUi ConfigUi;
        internal static DtrBarEntry DtrEntry;
        internal static bool FulfEnabled;
        internal List<BaseCommand> commands = new();

        public LazyLoot(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            config.Initialize(Service.PluginInterface);
            ConfigUi = new ConfigUi();
            Service.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigUi.IsOpen = true; };
            DtrEntry ??= Service.DtrBar.Get("LazyLoot");

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(BaseCommand)) && !t.IsAbstract))
            {
                try
                {
                    PluginLog.Information($"Initializing command '{t.Name}'.");
                    commands.Add((BaseCommand)Activator.CreateInstance(t)!);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed initializing command '{t.Name}'.");
                }
            }

            PluginLog.Information($"Loading all enabled commands.");

            foreach (var command in commands)
            {
                try
                {
                    if (command.Enabled)
                    {
                        command.Initialize();
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed load up command '{command}'.");
                }
            }

            Service.Framework.Update += OnFrameworkUpdate;
        }

        public string Name => "LazyLoot";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            PluginLog.Information(string.Format($">>Stop LazyLoot<<"));
            DtrEntry.Remove();

            foreach (var command in commands)
            {
                command.Dispose();
            }

            Service.Framework.Update -= OnFrameworkUpdate;
            Service.PluginInterface.SavePluginConfig(config);
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
    }
}