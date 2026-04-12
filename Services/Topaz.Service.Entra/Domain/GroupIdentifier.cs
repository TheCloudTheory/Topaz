namespace Topaz.Service.Entra.Domain;

public record GroupIdentifier(string Value)
{
    public static GroupIdentifier From(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value))
            : new GroupIdentifier(value);
    }

    public override string ToString() => Value;
}
