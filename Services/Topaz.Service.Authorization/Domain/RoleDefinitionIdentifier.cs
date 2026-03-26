namespace Topaz.Service.Authorization.Domain;

public record RoleDefinitionIdentifier(string Value)
{
    public static RoleDefinitionIdentifier From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) : new RoleDefinitionIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}