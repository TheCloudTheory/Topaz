namespace Topaz.Service.Shared.Domain;

public record ResourceGroupIdentifier(string Value)
{
    public static ResourceGroupIdentifier From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));    
        }
        
        return new ResourceGroupIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}