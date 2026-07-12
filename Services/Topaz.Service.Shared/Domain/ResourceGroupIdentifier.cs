namespace Topaz.Service.Shared.Domain;

public record ResourceGroupIdentifier(string Value)
{
    public static ResourceGroupIdentifier From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        if (value.Contains(".." ) || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("Resource group identifier contains forbidden characters.", nameof(value));
        return new ResourceGroupIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}