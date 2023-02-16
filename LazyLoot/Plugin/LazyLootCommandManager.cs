﻿using Dalamud.Game.Command;
using Dalamud.Plugin;
using LazyLoot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LazyLoot.Plugin
{
    public class LazyLootCommandManager<THost> : IDisposable
    {
        private readonly THost host;
        private readonly (string, CommandInfo)[] pluginCommands;

        public LazyLootCommandManager(THost host, DalamudPluginInterface pluginInterface)
        {
            this.host = host;

            pluginCommands = host.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(new Func<MethodInfo, IEnumerable<(string, CommandInfo)>>(GetCommandInfoTuple))
                .ToArray();

            Array.Reverse((Array)pluginCommands);
            AddCommandHandlers();
        }

        public void Dispose()
        {
            RemoveCommandHandlers();
            GC.SuppressFinalize(this);
        }

        private void AddCommandHandlers()
        {
            for (int index = 0; index < pluginCommands.Length; ++index)
            {
                (string, CommandInfo) pluginCommand = pluginCommands[index];
                Service.Service.CommandManager.AddHandler(pluginCommand.Item1, pluginCommand.Item2);
            }
        }

        private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(
          MethodInfo method)
        {
            CommandInfo.HandlerDelegate handlerDelegate = (CommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(CommandInfo.HandlerDelegate), host, method);
            CommandAttribute customAttribute1 = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
            AliasesAttribute customAttribute2 = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
            HelpMessageAttribute customAttribute3 = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
            DoNotShowInHelpAttribute customAttribute4 = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();
            CommandInfo commandInfo = new(handlerDelegate)
            {
                HelpMessage = customAttribute3?.HelpMessage ?? string.Empty,
                ShowInHelp = customAttribute4 == null
            };
            List<(string, CommandInfo)> valueTupleList = new()
            {
                (customAttribute1.Command, commandInfo)
            };
            List<(string, CommandInfo)> commandInfoTuple = valueTupleList;
            if (customAttribute2 != null)
            {
                for (int index = 0; index < customAttribute2.Aliases.Length; ++index)
                    commandInfoTuple.Add((customAttribute2.Aliases[index], commandInfo));
            }
            return commandInfoTuple;
        }

        private void RemoveCommandHandlers()
        {
            for (int index = 0; index < pluginCommands.Length; ++index)
                Service.Service.CommandManager.RemoveHandler(pluginCommands[index].Item1);
        }
    }
}