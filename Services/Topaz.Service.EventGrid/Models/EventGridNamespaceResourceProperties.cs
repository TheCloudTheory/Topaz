using System.Text.Json.Serialization;
using Topaz.Service.Shared;

namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridNamespaceResourceProperties
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState ProvisioningState { get; set; } = ProvisioningState.Succeeded;

    public List<InboundIpRule>? InboundIpRules { get; set; }
    public bool? IsZoneRedundant { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TlsVersion? MinimumTlsVersionAllowed { get; set; }
    public List<NamespacePrivateEndpointConnection>? PrivateEndpointConnections { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PublicNetworkAccess? PublicNetworkAccess { get; set; }

    public TopicSpacesConfiguration? TopicSpacesConfiguration { get; set; }
    public TopicsConfiguration? TopicsConfiguration { get; set; }

    public void UpdateFromRequest(EventGridNamespaceResourceProperties properties)
    {
        InboundIpRules = properties.InboundIpRules ?? InboundIpRules;
        IsZoneRedundant = properties.IsZoneRedundant ?? IsZoneRedundant;
        MinimumTlsVersionAllowed = properties.MinimumTlsVersionAllowed ?? MinimumTlsVersionAllowed;
        PrivateEndpointConnections = properties.PrivateEndpointConnections ?? PrivateEndpointConnections;
        PublicNetworkAccess = properties.PublicNetworkAccess ?? PublicNetworkAccess;
        TopicSpacesConfiguration = properties.TopicSpacesConfiguration ?? TopicSpacesConfiguration;
        TopicsConfiguration = properties.TopicsConfiguration ?? TopicsConfiguration;
    }

    public static EventGridNamespaceResourceProperties FromRequest(EventGridNamespaceResourceProperties properties)
    {
        return new EventGridNamespaceResourceProperties
        {
            InboundIpRules = properties.InboundIpRules,
            IsZoneRedundant = properties.IsZoneRedundant,
            MinimumTlsVersionAllowed = properties.MinimumTlsVersionAllowed,
            PrivateEndpointConnections = properties.PrivateEndpointConnections,
            PublicNetworkAccess = properties.PublicNetworkAccess,
            TopicSpacesConfiguration = properties.TopicSpacesConfiguration,
            TopicsConfiguration = properties.TopicsConfiguration
        };
    }
}