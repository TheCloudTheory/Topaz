namespace Topaz.Service.Entra.Domain;

public record ServicePrincipalIdentifier(string Value)
{
    public static ServicePrincipalIdentifier From(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value))
            : new ServicePrincipalIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}