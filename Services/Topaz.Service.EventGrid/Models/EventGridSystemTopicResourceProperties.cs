using System.Text.Json.Serialization;
using Topaz.Service.Shared;

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
}