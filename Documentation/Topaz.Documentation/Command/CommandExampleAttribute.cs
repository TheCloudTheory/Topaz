namespace Topaz.Documentation.Command;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CommandExampleAttribute(string title, string command) : Attribute
{
    public string Title { get; } = title;
    public string Command { get; } = command;
}