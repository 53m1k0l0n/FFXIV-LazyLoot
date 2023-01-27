using ImGuiNET;

namespace LazyLoot.Plugin
{
    public class PluginUI
    {
        public bool IsVisible;
        private string? preview;

        public void Draw()
        {
            if (!IsVisible || !ImGui.Begin("Lazy Loot Config", ref IsVisible, (ImGuiWindowFlags)96))
                return;
            ImGui.TextUnformatted("Features");
            if (ImGui.BeginTable("lootlootlootlootloot", 2))
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/need");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Roll need for everything. If impossible roll greed or pass if it's not up to need or greed");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/needonly");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Roll need for everything. If impossible, roll pass.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/greed");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Roll greed on all items.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/pass");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Pass on things you haven't rolled for yet.");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("/passall");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted("Passes on all, even if you rolled on them previously.");
                ImGui.EndTable();
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Display roll information in chat.", ref Plugin.config.EnableChatLogMessage);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Checkbox("Display roll information as toast.", ref Plugin.config.EnableToastMessage);
            ImGui.Spacing();
            ImGui.Separator();

            if (Plugin.config.EnableNormalToast)
            {
                preview = "Normal";
            }
            else if (Plugin.config.EnableErrorToast)
            {
                preview = "Error";
            }
            else
            {
                preview = "Quest";
            }

            if (ImGui.BeginCombo("Toast", preview))
            {
                if (ImGui.Selectable("Quest", ref Plugin.config.EnableQuestToast))
                {
                    preview = "Quest";
                    Plugin.config.EnableNormalToast = false;
                    Plugin.config.EnableErrorToast = false;
                }

                if (ImGui.Selectable("Normal", ref Plugin.config.EnableNormalToast))
                {
                    preview = "Normal";
                    Plugin.config.EnableQuestToast = false;
                    Plugin.config.EnableErrorToast = false;
                }

                if (ImGui.Selectable("Error", ref Plugin.config.EnableErrorToast))
                {
                    preview = "Error";
                    Plugin.config.EnableQuestToast = false;
                    Plugin.config.EnableNormalToast = false;
                }

                Plugin.config.Save();

                ImGui.EndCombo();
            }
            ImGui.End();
        }
    }
}
