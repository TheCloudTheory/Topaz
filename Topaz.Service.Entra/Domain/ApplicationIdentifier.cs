namespace Topaz.Service.Entra.Domain;

public record ApplicationIdentifier(string Value)
{
    public static ApplicationIdentifier From(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value))
            : new ApplicationIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}