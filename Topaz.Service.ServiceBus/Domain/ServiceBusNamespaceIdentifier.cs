namespace Topaz.Service.ServiceBus.Domain;

public sealed record ServiceBusNamespaceIdentifier(string Value)
{
    public static ServiceBusNamespaceIdentifier From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));    
        }
        
        return new ServiceBusNamespaceIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}