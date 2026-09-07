using System.Text.Json.Serialization;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridSystemTopicResourceProperties
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState ProvisioningState { get; set; } = ProvisioningState.Succeeded;
    
    public string? MetricResourceId { get; set; }
    public string? Source { get; set; }
    public string? TopicType { get; set; }

    public void UpdateFromRequest(EventGridSystemTopicResourceProperties request)
    {
        MetricResourceId = request.MetricResourceId ?? MetricResourceId;
        Source = request.Source ?? Source;
        TopicType = request.TopicType ?? TopicType;
    }

    public static EventGridSystemTopicResourceProperties FromRequest(EventGridSystemTopicResourceProperties request)
    {
        return new EventGridSystemTopicResourceProperties
        {
            MetricResourceId = request.MetricResourceId,
            Source = request.Source,
            TopicType = request.TopicType
        };
    }
}