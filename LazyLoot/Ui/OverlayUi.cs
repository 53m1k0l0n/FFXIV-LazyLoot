using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace LazyLoot.Ui
{
    internal class OverlayUi : Window
    {
        public OverlayUi() : base("Lazy Loot Autoloot Overlay",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.IsOpen = true;
            this.RespectCloseHotkey = false;
        }

        public override void Draw()
        {
            ImGui.SetWindowFontScale(1.5f);
            if (Plugin.LazyLoot.config.EnableNeedRoll)
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, "Autoloot Need");
            }
            else if (Plugin.LazyLoot.config.EnableNeedOnlyRoll)
            {
                ImGui.TextColored(ImGuiColors.TankBlue, "Autoloot Need only");
            }
            else if (Plugin.LazyLoot.config.EnableGreedRoll)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "Autoloot Greed");
            }

            this.Position = new Vector2(ImGuiHelpers.MainViewport.Size.X / 2 - ImGui.GetWindowSize().X / 2, 0) - Plugin.LazyLoot.config.OverlayOffset;
        }

        public override bool DrawConditions()
        {
            return Plugin.LazyLoot.config.EnableOverlay && Plugin.LazyLoot.autoLootEnabled;
        }

        public override void PostDraw()
        {
            ImGui.PopStyleVar();
        }

        public override void PreDraw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        }
    }
}