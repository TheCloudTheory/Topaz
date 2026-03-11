using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses.Topic;

internal sealed class ListServiceBusTopicsResponse
{
    public ServiceBusTopicResource[] Value { get; init; } = [];
    
    public static ListServiceBusTopicsResponse From(ServiceBusTopicResource[] topics)
    {
        return new ListServiceBusTopicsResponse
        {
            Value = topics
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}