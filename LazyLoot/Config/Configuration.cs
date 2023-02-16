using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System.Numerics;

namespace LazyLoot.Config
{
    public class Configuration : IPluginConfiguration
    {
        public bool EnableChatLogMessage = true;

        public bool EnableErrorToast = false;
        public bool EnableGreedRoll = false;
        public bool EnableNeedOnlyRoll = false;
        public bool EnableNeedRoll = true;
        public bool EnablePassRoll = false;
        public bool EnableNormalToast = false;
        public bool EnableOverlay = true;
        public bool EnableQuestToast = true;
        public bool EnableRollDelay = true;
        public bool EnableToastMessage = true;
        public Vector2 OverlayOffset = new(0, 0);
        public float RollDelayInSeconds = 1;

        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => pluginInterface.SavePluginConfig(this);
    }
}