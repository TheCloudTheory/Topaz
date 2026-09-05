using System.Text.Json.Serialization;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridTopicResourceProperties
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState ProvisioningState { get; set; } = ProvisioningState.Succeeded;

    public string? Endpoint { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PublicNetworkAccess? PublicNetworkAccess { get; set; } = Models.PublicNetworkAccess.Enabled;

    public List<InboundIpRule>? InboundIpRules { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InputSchema? InputSchema { get; set; } = Models.InputSchema.EventGridSchema;

    public bool? DisableLocalAuth { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TlsVersion? MinimumTlsVersionAllowed { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataResidencyBoundary? DataResidencyBoundary { get; set; }

    public string? MetricResourceId { get; set; }
    public EventTypeInfo? EventTypeInfo { get; set; }

    public void UpdateFromRequest(EventGridTopicResourceProperties properties)
    {
        PublicNetworkAccess = properties.PublicNetworkAccess ?? PublicNetworkAccess;
        InboundIpRules = properties.InboundIpRules ?? InboundIpRules;
        InputSchema = properties.InputSchema ?? InputSchema;
        DisableLocalAuth = properties.DisableLocalAuth ?? DisableLocalAuth;
        MinimumTlsVersionAllowed = properties.MinimumTlsVersionAllowed ?? MinimumTlsVersionAllowed;
        DataResidencyBoundary = properties.DataResidencyBoundary ?? DataResidencyBoundary;
        EventTypeInfo = properties.EventTypeInfo ?? EventTypeInfo;
    }

    public static EventGridTopicResourceProperties FromRequest(string topicName, EventGridTopicResourceProperties properties, SubscriptionIdentifier subscriptionIdentifier)
    {
        return new EventGridTopicResourceProperties
        {
            Endpoint = $"{GlobalSettings.GetEventGridEndpoint(topicName, subscriptionIdentifier.Value.ToString())}api/events",
            PublicNetworkAccess = properties.PublicNetworkAccess,
            InboundIpRules = properties.InboundIpRules,
            InputSchema = properties.InputSchema,
            DisableLocalAuth = properties.DisableLocalAuth,
            MinimumTlsVersionAllowed = properties.MinimumTlsVersionAllowed,
            DataResidencyBoundary = properties.DataResidencyBoundary,
            EventTypeInfo = properties.EventTypeInfo,
        };
    }
}

internal enum EventDefinitionKind
{
    Inline,
}

internal sealed class InlineEventProperties
{
    public string? DataSchemaUrl { get; set; }
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
    public string? DocumentationUrl { get; set; }
}

internal sealed class EventTypeInfo
{
    public Dictionary<string, InlineEventProperties>? InlineEventTypes { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventDefinitionKind? Kind { get; set; }
}

internal enum InputSchema
{
    EventGridSchema,
    CustomEventSchema,
    [JsonPropertyName("CloudEventSchemaV1_0")] CloudEventSchemaV1_0,
}

internal enum DataResidencyBoundary
{
    WithinGeopair,
    WithinRegion,
}