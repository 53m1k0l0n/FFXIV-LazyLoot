using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using LazyLoot.Services;
using System;
using System.Numerics;

namespace LazyLoot.Ui
{
    public class ConfigUi : Window, IDisposable
    {
        public string? rollPreview;
        internal WindowSystem windowSystem = new();
        private string? toastPreview;

        public ConfigUi() : base("Lazy Loot Config")
        {
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(400, 200),
                MaximumSize = new Vector2(99999, 99999),
            };
            windowSystem.AddWindow(this);
            Service.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        }

        public void Dispose()
        {
            Service.PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
            GC.SuppressFinalize(this);
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("config"))
            {
                if (ImGui.BeginTabItem("Features"))
                {
                    ImGui.BeginChild("generalFeatures");

                    DrawFeatures();
                    DrawRollingDelay();
                    DrawChatAndToast();
                    DrawFulf();

                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("User Restriction"))
                {
                    ImGui.BeginChild("generalFeatures");

                    DrawUserRestriction();

                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        public override void OnClose()
        {
            Plugin.LazyLoot.config.Save();
            Service.PluginInterface.UiBuilder.AddNotification("Configuration saved", "Lazy Loot", NotificationType.Success);
            base.OnClose();
        }

        private static void DrawFeatures()
        {
            ImGui.Text("/rolling need");
            ImGui.SameLine();
            ImGui.Text("Roll need for everything. If impossible roll greed or pass if greed is impossible.");
            ImGui.Separator();
            ImGui.Text("/rolling needonly");
            ImGui.SameLine();
            ImGui.Text("Roll need for everything. If impossible, roll pass.");
            ImGui.Separator();
            ImGui.Text("/rolling greed");
            ImGui.SameLine();
            ImGui.Text("Roll greed for everything. If impossible, roll pass.");
            ImGui.Separator();
            ImGui.Text("/rolling pass");
            ImGui.SameLine();
            ImGui.Text("Pass on things you haven't rolled for yet.");
            ImGui.Separator();
            ImGui.Text("/rolling passall");
            ImGui.SameLine();
            ImGui.Text("Passes on all, even if you rolled on them previously.");
            ImGui.Separator();
        }

        private static void DrawRollingDelay()
        {
            ImGui.Checkbox("Rolling delay between items:", ref Plugin.LazyLoot.config.EnableRollDelay);
            if (Plugin.LazyLoot.config.EnableRollDelay)
            {
                ImGui.SetNextItemWidth(100);
                ImGui.DragFloat("Min in seconds.", ref Plugin.LazyLoot.config.MinRollDelayInSeconds, 0.1F);

                if (Plugin.LazyLoot.config.MinRollDelayInSeconds >= Plugin.LazyLoot.config.MaxRollDelayInSeconds)
                {
                    Plugin.LazyLoot.config.MinRollDelayInSeconds = Plugin.LazyLoot.config.MaxRollDelayInSeconds - 0.1f;
                }

                if (Plugin.LazyLoot.config.MinRollDelayInSeconds < 0.5f)
                {
                    Plugin.LazyLoot.config.MinRollDelayInSeconds = 0.5f;
                }

                ImGui.SetNextItemWidth(100);
                ImGui.DragFloat("Max in seconds.", ref Plugin.LazyLoot.config.MaxRollDelayInSeconds, 0.1F);

                if (Plugin.LazyLoot.config.MaxRollDelayInSeconds <= Plugin.LazyLoot.config.MinRollDelayInSeconds)
                {
                    Plugin.LazyLoot.config.MaxRollDelayInSeconds = Plugin.LazyLoot.config.MinRollDelayInSeconds + 0.1f;
                }
            }
            ImGui.Separator();
        }

        private static void DrawUserRestriction()
        {
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

            ImGui.TextColored(ImGuiColors.DalamudRed, "Ignore items even if they are tradeable, do you want to trade them, don't ignore them.");
            ImGui.Checkbox("Ignore all items already unlocked. ( Cards, Music, Faded copy, Minions, Mounts, Emotes, Hairstyle )", ref Plugin.LazyLoot.config.RestrictionIgnoreItemUnlocked);
            ImGui.Checkbox("Ignore unlocked mounts.", ref Plugin.LazyLoot.config.RestrictionIgnoreMounts);
            ImGui.Checkbox("Ignore unlocked minions.", ref Plugin.LazyLoot.config.RestrictionIgnoreMinions);
            ImGui.Checkbox("Ignore unlocked bardings.", ref Plugin.LazyLoot.config.RestrictionIgnoreBardings);
            ImGui.Checkbox("Ignore unlocked triple triad cards.", ref Plugin.LazyLoot.config.RestrictionIgnoreTripleTriadCards);
            ImGui.Checkbox("Ignore unlocked emotes and hairstyle.", ref Plugin.LazyLoot.config.RestrictionIgnoreEmoteHairstyle);
            ImGui.Checkbox("Ignore unlocked orchestrion rolls.", ref Plugin.LazyLoot.config.RestrictionIgnoreOrchestrionRolls);
            ImGui.Checkbox("Ignore unlocked Faded Copy.", ref Plugin.LazyLoot.config.RestrictionIgnoreFadedCopy);

            ImGui.Separator();
            ImGui.Checkbox("Ignore items i can't use with atcual job.", ref Plugin.LazyLoot.config.RestrictionOtherJobItems);

            if (Plugin.LazyLoot.config.RestrictionIgnoreMounts
                && Plugin.LazyLoot.config.RestrictionIgnoreMinions
                && Plugin.LazyLoot.config.RestrictionIgnoreBardings
                && Plugin.LazyLoot.config.RestrictionIgnoreTripleTriadCards
                && Plugin.LazyLoot.config.RestrictionIgnoreEmoteHairstyle
                && Plugin.LazyLoot.config.RestrictionIgnoreOrchestrionRolls
                && Plugin.LazyLoot.config.RestrictionIgnoreFadedCopy)
            {
                Plugin.LazyLoot.config.RestrictionIgnoreItemUnlocked = true;
                Plugin.LazyLoot.config.RestrictionIgnoreMounts = false;
                Plugin.LazyLoot.config.RestrictionIgnoreMinions = false;
                Plugin.LazyLoot.config.RestrictionIgnoreBardings = false;
                Plugin.LazyLoot.config.RestrictionIgnoreTripleTriadCards = false;
                Plugin.LazyLoot.config.RestrictionIgnoreEmoteHairstyle = false;
                Plugin.LazyLoot.config.RestrictionIgnoreOrchestrionRolls = false;
                Plugin.LazyLoot.config.RestrictionIgnoreFadedCopy = false;
            }

            if (Plugin.LazyLoot.config.RestrictionIgnoreItemUnlocked)
            {
                Plugin.LazyLoot.config.RestrictionIgnoreMounts = false;
                Plugin.LazyLoot.config.RestrictionIgnoreMinions = false;
                Plugin.LazyLoot.config.RestrictionIgnoreBardings = false;
                Plugin.LazyLoot.config.RestrictionIgnoreTripleTriadCards = false;
                Plugin.LazyLoot.config.RestrictionIgnoreEmoteHairstyle = false;
                Plugin.LazyLoot.config.RestrictionIgnoreOrchestrionRolls = false;
                Plugin.LazyLoot.config.RestrictionIgnoreFadedCopy = false;
            }

            ImGui.Separator();
        }

        private void DrawChatAndToast()
        {
            ImGui.Checkbox("Display roll information in chat.", ref Plugin.LazyLoot.config.EnableChatLogMessage);
            ImGui.Checkbox("Display roll information as toast:", ref Plugin.LazyLoot.config.EnableToastMessage);
            if (Plugin.LazyLoot.config.EnableToastMessage)
            {
                ImGui.SameLine();

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

            ImGui.Text("Options are persistent");

            if (Plugin.LazyLoot.config.EnableNeedOnlyRoll)
            {
                rollPreview = "Need only";
            }
            else if (Plugin.LazyLoot.config.EnableGreedRoll)
            {
                rollPreview = "Greed only";
            }
            else if (Plugin.LazyLoot.config.EnablePassRoll)
            {
                rollPreview = "Pass";
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
                    Plugin.LazyLoot.config.EnableGreedRoll = false;
                }

                ImGui.EndCombo();
            }

            ImGui.Text("First Roll Delay Range (In seconds)");
            ImGui.SetNextItemWidth(100);
            ImGui.DragFloat("Min in seconds. ", ref Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds, 0.1F);

            if (Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds >= Plugin.LazyLoot.config.FulfFinalRollDelayInSeconds)
            {
                Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds = Plugin.LazyLoot.config.FulfFinalRollDelayInSeconds - 0.1f;
            }

            if (Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds < 1.5f)
            {
                Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds = 1.5f;
            }

            ImGui.SetNextItemWidth(100);
            ImGui.DragFloat("Max in seconds. ", ref Plugin.LazyLoot.config.FulfFinalRollDelayInSeconds, 0.1F);

            if (Plugin.LazyLoot.config.FulfFinalRollDelayInSeconds <= Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds)
            {
                Plugin.LazyLoot.config.FulfFinalRollDelayInSeconds = Plugin.LazyLoot.config.FulfInitialRollDelayInSeconds + 0.1f;
            }
            ImGui.Separator();
        }
    }
}
