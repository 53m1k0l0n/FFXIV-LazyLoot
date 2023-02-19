using Dalamud.Interface.Colors;
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
        private string? toastPreview;

        public ConfigUi() : base("Lazy Loot Config", ImGuiWindowFlags.AlwaysAutoResize)
        {
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(400, 200),
                MaximumSize = new Vector2(99999, 99999),
            };
            windowSystem.AddWindow(this);
            Service.Service.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        }

        public void Dispose()
        {
            Service.Service.PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
            GC.SuppressFinalize(this);
        }

        public override void Draw()
        {
            DrawFeatures();
            DrawRollingDelay();
            DrawChatAndToast();
            DrawUserRestriction();
            DrawFulf();
            DrawSave();
        }

        public override void OnClose()
        {
            Plugin.LazyLoot.config.Save();
            Service.Service.PluginInterface.UiBuilder.AddNotification("Configuration saved", "Lazy Loot", NotificationType.Success);
            base.OnClose();
        }

        private static void DrawFeatures()
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
            ImGui.Text("Roll greed for everything. If impossible, roll pass.");
            ImGui.Separator();
            ImGui.Text("/roll pass");
            ImGui.SameLine();
            ImGui.Text("Pass on things you haven't rolled for yet.");
            ImGui.Separator();
            ImGui.Text("/roll passall");
            ImGui.SameLine();
            ImGui.Text("Passes on all, even if you rolled on them previously.");
            ImGui.Separator();
        }

        private static void DrawRollingDelay()
        {
            ImGui.Checkbox("Rolling delay between items:", ref Plugin.LazyLoot.config.EnableRollDelay);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (Plugin.LazyLoot.config.EnableRollDelay)
            {
                ImGui.DragFloat("in seconds.", ref Plugin.LazyLoot.config.RollDelayInSeconds, 0.1F);

                if (Plugin.LazyLoot.config.RollDelayInSeconds < 0.1f)
                {
                    Plugin.LazyLoot.config.RollDelayInSeconds = 0.1f;
                }

                ImGui.Separator();
            }
        }

        private static void DrawSave()
        {
            if (ImGui.Button("Save"))
            {
                Plugin.LazyLoot.config.Save();
                Service.Service.PluginInterface.UiBuilder.AddNotification("Configuration saved", "Lazy Loot", NotificationType.Success);
            }
            ImGui.SameLine();
            if (ImGui.Button("Save and Close"))
            {
                Plugin.LazyLoot.ConfigUi.IsOpen = false;
            }
        }

        private static void DrawUserRestriction()
        {
            ImGui.Text("User Restriction");
            ImGui.Separator();
            ImGui.Checkbox("Ignore item Level below:", ref Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelow);
            if (Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelow)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                ImGui.DragInt("ILvl", ref Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelowValue);

                if (Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelowValue < 0)
                {
                    Plugin.LazyLoot.config.RestrictionIgnoreItemLevelBelowValue = 0;
                }
            }
            ImGui.Checkbox("Ignore items already unlocked. ( Cards, Music, Faded copy, Minions, Mounts )", ref Plugin.LazyLoot.config.RestrictionIgnoreItemUnlocked);

            ImGui.Separator();
        }

        private void DrawChatAndToast()
        {
            ImGui.Checkbox("Display roll information in chat.", ref Plugin.LazyLoot.config.EnableChatLogMessage);
            ImGui.Checkbox("Display roll information as toast:", ref Plugin.LazyLoot.config.EnableToastMessage);
            ImGui.SameLine();
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
                ImGui.SetNextItemWidth(100);
                if (ImGui.BeginCombo(string.Empty, toastPreview))
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

            ImGui.Separator();
        }

        private void DrawFulf()
        {
            ImGui.Text("Fancy Ultimate Lazy Feature. Enable or Disable with /fulf  (Not persistent).");
            ImGui.TextColored(Plugin.LazyLoot.FulfEnabled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, "FULF");
            if (Plugin.LazyLoot.FulfEnabled)
            {
                ImGui.Text("Options are persistent");

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
                        Plugin.LazyLoot.config.EnablePassRoll = false;
                    }

                    if (ImGui.Selectable("Need only", ref Plugin.LazyLoot.config.EnableNeedOnlyRoll))
                    {
                        Plugin.LazyLoot.config.EnableNeedRoll = false;
                        Plugin.LazyLoot.config.EnableGreedRoll = false;
                        Plugin.LazyLoot.config.EnablePassRoll = false;
                    }

                    if (ImGui.Selectable("Greed", ref Plugin.LazyLoot.config.EnableGreedRoll))
                    {
                        Plugin.LazyLoot.config.EnableNeedRoll = false;
                        Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                        Plugin.LazyLoot.config.EnablePassRoll = false;
                    }

                    if (ImGui.Selectable("Pass", ref Plugin.LazyLoot.config.EnablePassRoll))
                    {
                        Plugin.LazyLoot.config.EnableNeedRoll = false;
                        Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                    }

                    ImGui.EndCombo();
                }

                ImGui.Spacing();
                ImGui.Text("Delay before Fulf will roll on items. Just to be sure that all chest are open.");
                ImGui.Text("Doesn't matter most of the time, it's more for stuff like some Normal Raids and Alli Raids.");
                ImGui.Text("Be careful if you set it too low, Fulf wont roll on all items.");
                ImGui.DragInt("seconds", ref Plugin.LazyLoot.config.FulfDelay);
            }

            ImGui.Separator();
        }
    }
}