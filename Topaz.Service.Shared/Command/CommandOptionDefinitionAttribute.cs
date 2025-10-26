namespace Topaz.Service.Shared.Command;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandOptionDefinitionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}