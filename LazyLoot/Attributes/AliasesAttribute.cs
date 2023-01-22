
// Type: DalamudPluginProjectTemplate.Attributes.AliasesAttribute




using System;

namespace LootMaster.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AliasesAttribute : Attribute
    {
        public string[] Aliases { get; }

        public AliasesAttribute(params string[] aliases) => Aliases = aliases;
    }
}
