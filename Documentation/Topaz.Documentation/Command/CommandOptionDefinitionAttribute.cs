namespace Topaz.Documentation.Command;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandOptionDefinitionAttribute(string description, bool required = false) : Attribute
{
    public string Description { get; } = description;
    public bool  Required { get; } =  required;
}