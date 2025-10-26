namespace Topaz.Documentation.Command;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandDefinitionAttribute(string commandName, string description) : Attribute
{
    public string CommandName { get; } = commandName;
    public string Description { get; } = description;
}