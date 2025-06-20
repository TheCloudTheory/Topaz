namespace Topaz.Service.Shared.Domain;

public record SubscriptionIdentifier(Guid Value)
{
    public static SubscriptionIdentifier From(Guid value)
    {
        return new SubscriptionIdentifier(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}