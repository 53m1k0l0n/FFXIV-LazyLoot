using Dalamud;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Internal.Notifications;
using LazyLoot.Attributes;
using LazyLoot.Services;

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
                    Service.ToastGui.ShowQuest("FULF enabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    Service.ChatGui.CheckMessageHandled += NoticeLoot;
                    SetRollOption(subArguments[0]);
                    if (Service.Condition[ConditionFlag.BoundByDuty])
                    {
                        Roll(string.Empty, SetFulfArguments());
                    }
                }
                else
                {
                    Service.ToastGui.ShowQuest("FULF disabled", new QuestToastOptions() { DisplayCheckmark = true, PlaySound = true });
                    Service.ChatGui.CheckMessageHandled -= NoticeLoot;
                }
            }

            if (subArguments.Length > 1)
            {
                SetRollOption(subArguments[1]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            Service.ChatGui.CheckMessageHandled -= NoticeLoot;
            base.Dispose(disposing);
        }

        public void NoticeLoot(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (isRolling) return;
            if ((ushort)type != 2105) return;
            if (message.TextValue == Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "Bitte um das Beutegut würfeln.",
                ClientLanguage.French => "Veuillez lancer les dés pour le butin.",
                ClientLanguage.Japanese => "ロットを行ってください。",
                _ => "Cast your lot."
            })
            {
                Service.PluginInterface.UiBuilder.AddNotification(">>New Loot<<", "Lazy Loot", NotificationType.Info);
                Roll(string.Empty, SetFulfArguments());
            }
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