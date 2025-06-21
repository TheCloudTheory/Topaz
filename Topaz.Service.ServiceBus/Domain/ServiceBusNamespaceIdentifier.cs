namespace Topaz.Service.ServiceBus.Domain;

public sealed record ServiceBusNamespaceIdentifier(string Value)
{
    public static ServiceBusNamespaceIdentifier From(string value)
    {
        return new ServiceBusNamespaceIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}