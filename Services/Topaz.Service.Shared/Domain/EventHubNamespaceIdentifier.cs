namespace Topaz.Service.Shared.Domain;

public sealed record EventHubNamespaceIdentifier(string Value)
{
    public static EventHubNamespaceIdentifier From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));    
        }
        
        return new EventHubNamespaceIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}