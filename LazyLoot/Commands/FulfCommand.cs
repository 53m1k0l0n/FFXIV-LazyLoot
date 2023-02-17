using Dalamud.Game.Gui.Toast;
using LazyLoot.Attributes;

namespace LazyLoot.Commands
{
    public class FulfCommand : RollCommand
    {
        [Command("/fulf", "En/Disable FULF with /fulf or change the loot rule with /fulf c need | needonly | greed or pass .")]
        public void EnDisableFluf(string command, string arguments)
        {
            var subArguments = arguments.Split(' ');

            if (subArguments[0] != "c")
            {
                Plugin.LazyLoot.FulfEnabled = !Plugin.LazyLoot.FulfEnabled;

                if (Plugin.LazyLoot.FulfEnabled)
                {
                    Service.Service.ToastGui.ShowQuest("FULF enabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    Service.Service.ChatGui.CheckMessageHandled += NoticeLoot;
                }
                else
                {
                    Service.Service.ToastGui.ShowQuest("FULF disabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    Service.Service.ChatGui.CheckMessageHandled -= NoticeLoot;
                }
            }

            if (subArguments.Length > 1)
            {
                SetRollOption(subArguments[1]);
            }
            else
            {
                SetRollOption(subArguments[0]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            Service.Service.ChatGui.CheckMessageHandled -= NoticeLoot;
            base.Dispose(disposing);
        }

        public void SetRollOption(string subArgument)
        {
            switch (subArgument)
            {
                case "need":
                    Plugin.LazyLoot.config.EnableNeedRoll = true;
                    Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                    Plugin.LazyLoot.config.EnableGreedRoll = false;
                    Plugin.LazyLoot.config.EnablePassRoll = false;
                    break;

                case "needonly":
                    Plugin.LazyLoot.config.EnableNeedRoll = false;
                    Plugin.LazyLoot.config.EnableNeedOnlyRoll = true;
                    Plugin.LazyLoot.config.EnableGreedRoll = false;
                    Plugin.LazyLoot.config.EnablePassRoll = false;
                    break;

                case "greed":
                    Plugin.LazyLoot.config.EnableNeedRoll = false;
                    Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                    Plugin.LazyLoot.config.EnableGreedRoll = true;
                    Plugin.LazyLoot.config.EnablePassRoll = false;
                    break;

                case "pass":
                    Plugin.LazyLoot.config.EnableNeedRoll = false;
                    Plugin.LazyLoot.config.EnableNeedOnlyRoll = false;
                    Plugin.LazyLoot.config.EnableGreedRoll = false;
                    Plugin.LazyLoot.config.EnablePassRoll = true;
                    break;
            }
        }
    }
}