﻿using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace LazyLoot.Ui
{
    public class ConfigUi : Window, IDisposable
    {
        public string? rollPreview;
        internal WindowSystem windowSystem = new();
        private readonly Plugin.LazyLoot lazyLoot;
        private string? toastPreview;

        public ConfigUi(Plugin.LazyLoot lazyLoot) : base("Lazy Loot Config", ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.lazyLoot = lazyLoot;
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(400, 200),
                MaximumSize = new Vector2(99999, 99999),
            };
            windowSystem.AddWindow(this);
            Plugin.LazyLoot.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        }

        public void Dispose()
        {
            Plugin.LazyLoot.PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
            GC.SuppressFinalize(this);
        }

        public override void Draw()
        {
            ImGui.Text("Features");
            ImGui.Separator();
            ImGui.Text("/roll need");
            ImGui.SameLine();
            ImGui.Text("Roll need for everything. If impossible roll greed or pass if greed is impossible.");
            ImGui.Separator();
            ImGui.Text("/roll needonly");
            ImGui.SameLine();
            ImGui.Text("Roll need for everything. If impossible, roll pass.");
            ImGui.Separator();
            ImGui.Text("/roll greed");
            ImGui.SameLine();
            ImGui.Text("Roll greed on all items or pass if greed is impossible.");
            ImGui.Separator();
            ImGui.Text("/roll pass");
            ImGui.SameLine();
            ImGui.Text("Pass on things you haven't rolled for yet.");
            ImGui.Separator();
            ImGui.Text("/roll passall");
            ImGui.SameLine();
            ImGui.Text("Passes on all, even if you rolled on them previously.");
            ImGui.Separator();

            ImGui.Checkbox("Rolling delay between items.", ref Plugin.LazyLoot.config.EnableRollDelay);

            if (Plugin.LazyLoot.config.EnableRollDelay)
            {
                ImGui.DragFloat("Delay in seconds.", ref Plugin.LazyLoot.config.RollDelayInSeconds, 0.1F);

                if (Plugin.LazyLoot.config.RollDelayInSeconds < 0.1f)
                {
                    Plugin.LazyLoot.config.RollDelayInSeconds = 0.1f;
                }

                ImGui.Separator();
            }
            ImGui.Checkbox("Display roll information in chat.", ref Plugin.LazyLoot.config.EnableChatLogMessage);
            ImGui.Checkbox("Display roll information as toast.", ref Plugin.LazyLoot.config.EnableToastMessage);

            if (Plugin.LazyLoot.config.EnableToastMessage)
            {
                if (Plugin.LazyLoot.config.EnableNormalToast)
                {
                    toastPreview = "Normal";
                }
                else if (Plugin.LazyLoot.config.EnableErrorToast)
                {
                    toastPreview = "Error";
                }
                else
                {
                    toastPreview = "Quest";
                }

                if (ImGui.BeginCombo("Toast", toastPreview))
                {
                    if (ImGui.Selectable("Quest", ref Plugin.LazyLoot.config.EnableQuestToast))
                    {
                        Plugin.LazyLoot.config.EnableNormalToast = false;
                        Plugin.LazyLoot.config.EnableErrorToast = false;
                    }

                    if (ImGui.Selectable("Normal", ref Plugin.LazyLoot.config.EnableNormalToast))
                    {
                        Plugin.LazyLoot.config.EnableQuestToast = false;
                        Plugin.LazyLoot.config.EnableErrorToast = false;
                    }

                    if (ImGui.Selectable("Error", ref Plugin.LazyLoot.config.EnableErrorToast))
                    {
                        Plugin.LazyLoot.config.EnableQuestToast = false;
                        Plugin.LazyLoot.config.EnableNormalToast = false;
                    }

                    ImGui.EndCombo();
                }
            }

            ImGui.Spacing();

            ImGui.Text("Fancy Ultimate Lazy Feature. Enable or Disable with /fulf  (Not persistent), Status with /fulf?.");
            ImGui.TextColored(Plugin.LazyLoot.flufEnabled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, "FULF");
            if (Plugin.LazyLoot.flufEnabled)
            {
                ImGui.Text("Options are persistent");
                ImGui.Checkbox("Enable overlay when 'FULF' is enabled", ref Plugin.LazyLoot.config.EnableOverlay);
                if (Plugin.LazyLoot.config.EnableOverlay)
                {
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragFloat2("Overlay offset", ref Plugin.LazyLoot.config.OverlayOffset);

                    if (Plugin.LazyLoot.config.EnableNeedOnlyRoll)
                    {
                        rollPreview = "Need only";
                    }
                    else if (Plugin.LazyLoot.config.EnableGreedRoll)
                    {
                        rollPreview = "Greed";
                    }
                    else
                    {
                        rollPreview = "Need";
                    }

                    if (ImGui.BeginCombo("Roll options", rollPreview))
                    {
                        if (ImGui.Selectable("Need", ref Plugin.LazyLoot.config.EnableNeedRoll))
                        {
                            Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                            Plugin.LazyLoot.config.EnableGreedRoll = false;
                        }

                        if (ImGui.Selectable("Need only", ref Plugin.LazyLoot.config.EnableNeedOnlyRoll))
                        {
                            Plugin.LazyLoot.config.EnableNeedRoll = false;
                            Plugin.LazyLoot.config.EnableGreedRoll = false;
                        }

                        if (ImGui.Selectable("Greed", ref Plugin.LazyLoot.config.EnableGreedRoll))
                        {
                            Plugin.LazyLoot.config.EnableNeedRoll = false;
                            Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                        }

                        ImGui.EndCombo();
                    }
                }
            }

            ImGui.Separator();
            if (ImGui.Button("Save"))
            {
                Plugin.LazyLoot.config.Save();
                Plugin.LazyLoot.PluginInterface.UiBuilder.AddNotification("Configuration saved", "Lazy Loot", NotificationType.Success);
            }
            ImGui.SameLine();
            if (ImGui.Button("Save and Close"))
            {
                lazyLoot.configUi.IsOpen = false;
            }
        }

        public override void OnClose()
        {
            Plugin.LazyLoot.config.Save();
            Plugin.LazyLoot.PluginInterface.UiBuilder.AddNotification("Configuration saved", "Lazy Loot", NotificationType.Success);
            base.OnClose();
        }
    }
}