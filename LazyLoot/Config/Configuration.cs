using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace LazyLoot.Config
{
    public class Configuration : IPluginConfiguration
    {
        public bool EnableChatLogMessage = true;
        public bool EnableToastMessage = true;
        public bool EnableNormalToast = false;
        public bool EnableErrorToast = false;
        public bool EnableQuestToast = true;
        public bool EnableRollDelay = true;
        public int RollDelayInSeconds = 2;

        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => pluginInterface.SavePluginConfig(this);
    }
}
