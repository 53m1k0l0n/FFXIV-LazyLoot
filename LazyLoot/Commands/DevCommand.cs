using Dalamud.Logging;
using LazyLoot.Attributes;
using Lumina.Excel.GeneratedSheets;

namespace LazyLoot.Commands
{
#if DEBUG
    public class DevCommand : BaseCommand
    {
        [Command("/lldev", "Dev Command", false)]
        public void DevCommandRun(string command, string arguments)
        {
            var itemSheet = Services.Service.Data.Excel.GetSheet<Item>()!;

            foreach (var item in itemSheet)
            {

                var itemAction = item.ItemAction.Value;
                if (itemAction == null) continue;

                if (item.Name.RawString.StartsWith("Faded Copy "))
                {
                    PluginLog.Information($"Name: {item.Name} Row: {itemAction.RowId} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                }

                ////switch (itemAction.Type)
                ////{
                ////    case 0xA49:  // Unlock Link (Emote, Hairstyle)
                ////        PluginLog.Information($"Emote/Hairstyle: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x355:  // Minions
                ////        PluginLog.Information($"Minions: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x3F5:  // Bardings
                ////        PluginLog.Information($"Bardings: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x52A:  // Mounts
                ////        PluginLog.Information($"Mounts: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0xD1D:  // Triple Triad Cards
                ////        PluginLog.Information($"Triple Triad Cards: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x4E76: // Ornaments
                ////        PluginLog.Information($"Ornaments: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    case 0x625F: // Orchestrion Rolls
                ////        PluginLog.Information($"Orchestrion Rolls: Name: {item.Name} Type: {itemAction.Type} IsUnique: {item.IsUnique} IsUntradeable: {item.IsUntradable}");
                ////        break;

                ////    default:
                ////        continue;
                ////}
            }
        }
    }
#endif
}
