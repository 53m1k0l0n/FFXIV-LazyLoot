using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace LootMaster.Config
{
    public class Configuration : IPluginConfiguration
    {
        public bool EnableChatLogMessage = true;
        public bool EnableToastMessage = true;
        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => pluginInterface.SavePluginConfig(this);
    }
}
