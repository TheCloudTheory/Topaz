using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses.Queue;

internal sealed class ListServiceBusQueuesResponse
{
    public ServiceBusQueueResource[] Value { get; init; } = [];
    
    public static ListServiceBusQueuesResponse From(ServiceBusQueueResource[] queues)
    {
        return new ListServiceBusQueuesResponse
        {
            Value = queues
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}