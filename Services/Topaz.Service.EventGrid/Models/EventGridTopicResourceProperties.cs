using System.Text.Json.Serialization;
using Topaz.Service.Shared;

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

    public void UpdateFromRequest(EventGridTopicResourceProperties properties)
    {
        PublicNetworkAccess = properties.PublicNetworkAccess ?? PublicNetworkAccess;
        InboundIpRules = properties.InboundIpRules ?? InboundIpRules;
        InputSchema = properties.InputSchema ?? InputSchema;
        DisableLocalAuth = properties.DisableLocalAuth ?? DisableLocalAuth;
        MinimumTlsVersionAllowed = properties.MinimumTlsVersionAllowed ?? MinimumTlsVersionAllowed;
        DataResidencyBoundary = properties.DataResidencyBoundary ?? DataResidencyBoundary;
    }

    public static EventGridTopicResourceProperties FromRequest(EventGridTopicResourceProperties properties)
    {
        return new EventGridTopicResourceProperties
        {
            PublicNetworkAccess = properties.PublicNetworkAccess,
            InboundIpRules = properties.InboundIpRules,
            InputSchema = properties.InputSchema,
            DisableLocalAuth = properties.DisableLocalAuth,
            MinimumTlsVersionAllowed = properties.MinimumTlsVersionAllowed,
            DataResidencyBoundary = properties.DataResidencyBoundary,
        };
    }
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