using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses;

internal sealed class ListServiceBusNamespacesResponse
{
    public ServiceBusNamespaceResource[] Value { get; init; } = [];

    public static ListServiceBusNamespacesResponse From(ServiceBusNamespaceResource[] namespaces)
    {
        return new ListServiceBusNamespacesResponse
        {
            Value = namespaces
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}