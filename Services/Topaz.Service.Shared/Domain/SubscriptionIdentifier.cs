namespace Topaz.Service.Shared.Domain;

public record SubscriptionIdentifier(Guid Value)
{
    public static readonly SubscriptionIdentifier Empty = new(Guid.Empty);

    public static SubscriptionIdentifier From(Guid value)
    {
        return new SubscriptionIdentifier(value);
    }
    
    public static SubscriptionIdentifier From(string? value)
    {
        return !Guid.TryParse(value, out var id)
            ? throw new ArgumentException($"Invalid subscription identifier value '{value}'", nameof(value))
            : new SubscriptionIdentifier(id);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}