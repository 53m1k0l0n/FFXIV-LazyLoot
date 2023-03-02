using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace LazyLoot.Config
{
    public class Configuration : IPluginConfiguration
    {
        // Output
        public bool EnableChatLogMessage = true;
        public bool EnableToastMessage = true;
        public bool EnableErrorToast = false;
        public bool EnableNormalToast = false;
        public bool EnableQuestToast = true;

        // FulfRollOption
        public bool EnableGreedRoll = false;
        public bool EnableNeedOnlyRoll = false;
        public bool EnableNeedRoll = true;
        public bool EnablePassRoll = false;

        // RollDelay
        public bool EnableRollDelay = true;
        public float MinRollDelayInSeconds = 0.5f;
        public float MaxRollDelayInSeconds = 1f;
        public float FulfInitialRollDelayInSeconds = 1.5f;
        public float FulfFinalRollDelayInSeconds = 3f;

        // Restrictions
        // ILvl
        public bool RestrictionIgnoreItemLevelBelow = false;
        public int RestrictionIgnoreItemLevelBelowValue = 0;
        // AllItems
        public bool RestrictionIgnoreItemUnlocked = false;
        // Mounts        
        public bool RestrictionIgnoreMounts = false;
        // Minnions
        public bool RestrictionIgnoreMinions = false;
        // Bardings
        public bool RestrictionIgnoreBardings = false;
        // TripleTriadCards
        public bool RestrictionIgnoreTripleTriadCards = false;
        // Emote/Hairstyle
        public bool RestrictionIgnoreEmoteHairstyle = false;
        // OrchestrionRolls
        public bool RestrictionIgnoreOrchestrionRolls = false;
        // FadedCopy
        public bool RestrictionIgnoreFadedCopy = false;
        // Items i can't use with actuall class
        public bool RestrictionOtherJobItems = false;


        [JsonIgnore]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => pluginInterface.SavePluginConfig(this);
    }
}