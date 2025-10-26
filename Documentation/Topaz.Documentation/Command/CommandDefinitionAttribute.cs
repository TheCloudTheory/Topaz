namespace Topaz.Documentation.Command;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandDefinitionAttribute(string commandName, string commandGroup, string description) : Attribute
{
    public string CommandName { get; } = commandName;
    public string CommandGroup { get; } = commandGroup;
    public string Description { get; } = description;
}