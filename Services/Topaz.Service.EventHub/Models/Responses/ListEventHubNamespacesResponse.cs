using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Models.Responses;

internal sealed class ListEventHubNamespacesResponse
{
    public EventHubNamespaceResource[] Value { get; init; } = [];

    public static ListEventHubNamespacesResponse From(EventHubNamespaceResource[] namespaces) =>
        new() { Value = namespaces };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
