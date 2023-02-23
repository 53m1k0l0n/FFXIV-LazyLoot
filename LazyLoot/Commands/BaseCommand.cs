using Dalamud.Game.Command;
using LazyLoot.Attributes;
using LazyLoot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LazyLoot.Commands
{
    public abstract class BaseCommand : IDisposable
    {
        public virtual bool Enabled => true;

        internal IEnumerable<MethodInfo> SlashCommands => GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.CustomAttributes.Any(ca => ca.AttributeType == typeof(CommandAttribute)));

        public virtual void Initialize()
        {
            foreach (var methodInfo in SlashCommands)
            {
                var attr = (CommandAttribute?)methodInfo.GetCustomAttribute(typeof(CommandAttribute));
                if (attr == null || Delegate.CreateDelegate(typeof(CommandInfo.HandlerDelegate), this, methodInfo, false) == null)
                    continue;

                if (!Service.CommandManager.Commands.ContainsKey(attr.Command))
                {
                    Service.CommandManager.AddHandler(attr.Command, new CommandInfo((string command, string argument) => // HandlerDelegate
                    {
                        methodInfo.Invoke(this, new string[] { command, argument });
                    })
                    {
                        HelpMessage = attr.HelpMessage,
                        ShowInHelp = attr.ShowInHelp,
                    });
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            foreach (var methodInfo in SlashCommands)
            {
                var attr = (CommandAttribute?)methodInfo.GetCustomAttribute(typeof(CommandAttribute));
                if (attr == null || Delegate.CreateDelegate(typeof(CommandInfo.HandlerDelegate), this, methodInfo, false) == null)
                    continue;

                if (Service.CommandManager.Commands.ContainsKey(attr.Command))
                {
                    Service.CommandManager.RemoveHandler(attr.Command);
                }
            }
        }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}