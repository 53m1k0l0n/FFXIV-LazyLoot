using System;

namespace LazyLoot.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Command { get; }
        public string HelpMessage { get; }
        public bool ShowInHelp { get; }

        public CommandAttribute(string Command, string HelpMessage = "", bool showInHelp = true)
        {
            this.Command = Command;
            this.HelpMessage = HelpMessage;
            this.ShowInHelp = showInHelp;
        }
    }
}