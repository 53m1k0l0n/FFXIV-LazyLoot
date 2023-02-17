using LazyLoot.Attributes;

namespace LazyLoot.Commands
{
    public class OpenConfigCommand : BaseCommand
    {
        [Command("/lazy", "Open Lazy Loot config.")]
        public void OpenConfig(string command, string arguments)
        {
            Plugin.LazyLoot.ConfigUi.IsOpen = !Plugin.LazyLoot.ConfigUi.IsOpen;
        }
    }
}