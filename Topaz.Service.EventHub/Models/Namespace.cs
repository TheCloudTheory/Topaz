namespace Topaz.Service.EventHub.Models;

public class Namespace
{
    public string Id => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.EventHub/namespaces/{Name}";
    public string? Name { get; init; }
    public string? ResourceGroup { get; init; }
    public string? Location { get; init; }
    public string? SubscriptionId { get; init; }
}