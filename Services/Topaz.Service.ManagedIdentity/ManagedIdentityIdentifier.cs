namespace Topaz.Service.ManagedIdentity;

public record ManagedIdentityIdentifier(string Value)
{
    public static ManagedIdentityIdentifier From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) : new ManagedIdentityIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}