namespace Topaz.Service.Entra.Domain;

public record UserIdentifier(string Value)
{
    public static UserIdentifier From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) : new UserIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}