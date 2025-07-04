namespace Topaz.Service.Shared.Domain;

public record SubscriptionIdentifier(Guid Value)
{
    public static SubscriptionIdentifier From(Guid value)
    {
        return new SubscriptionIdentifier(value);
    }
    
    public static SubscriptionIdentifier From(string? value)
    {
        if (Guid.TryParse(value, out var id) == false)
        {
            throw new ArgumentException($"Invalid subscription identifier value '{value}'", nameof(value));
        }
        
        return new SubscriptionIdentifier(id);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}