namespace Topaz.Service.EventHub.Models;

public sealed class EventHub
{
    public string Id => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.EventHub/namespaces/{NamespaceName}/eventhubs/{Name}";
    public string? Name { get; init; }
    private string? NamespaceName { get; init; }
    private string? ResourceGroup { get; init; }
    private string? SubscriptionId { get; init; }

    public static EventHub New(string name, string namespaceName, string resourceGroup, string subscriptionId)
    {
        return new EventHub()
        {
            Name = name,
            NamespaceName = namespaceName,
            ResourceGroup = resourceGroup,
            SubscriptionId = subscriptionId
        };
    }
}