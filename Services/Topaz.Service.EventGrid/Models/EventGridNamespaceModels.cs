using System.Text.Json.Serialization;

namespace Topaz.Service.EventGrid.Models;

public enum TlsVersion
{
    [JsonPropertyName("1.0")] Tls10,
    [JsonPropertyName("1.1")] Tls11,
    [JsonPropertyName("1.2")] Tls12,
}

public enum PublicNetworkAccess
{
    Enabled,
    Disabled,
}

public enum IpActionType
{
    Allow,
}

public enum TopicSpacesConfigurationState
{
    Disabled,
    Enabled,
}

public enum CustomDomainValidationState
{
    Pending,
    Approved,
    ErrorRetrievingDnsRecord,
}

public enum CustomDomainIdentityType
{
    SystemAssigned,
    UserAssigned,
}

public enum RoutingIdentityType
{
    None,
    SystemAssigned,
    UserAssigned,
}

public enum PersistedConnectionStatus
{
    Pending,
    Approved,
    Rejected,
    Disconnected,
}

public enum ResourceProvisioningState
{
    Creating,
    Updating,
    Deleting,
    Succeeded,
    Canceled,
    Failed,
}

internal sealed class InboundIpRule
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IpActionType? Action { get; set; }

    public string? IpMask { get; set; }
}

internal sealed class TopicSpacesConfiguration
{
    public List<CustomDomainConfiguration>? CustomDomains { get; set; }
    public string? Hostname { get; set; }
    public int? MaximumClientSessionsPerAuthenticationName { get; set; }
    public int? MaximumSessionExpiryInHours { get; set; }
    public string? RouteTopicResourceId { get; set; }
    public RoutingEnrichments? RoutingEnrichments { get; set; }
    public RoutingIdentityInfo? RoutingIdentityInfo { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TopicSpacesConfigurationState? State { get; set; }
}

internal sealed class TopicsConfiguration
{
    public List<CustomDomainConfiguration>? CustomDomains { get; set; }
    public string? Hostname { get; set; }
}

internal sealed class CustomDomainConfiguration
{
    public string? CertificateUrl { get; set; }

    public string? ExpectedTxtRecordName { get; set; }

    public string? ExpectedTxtRecordValue { get; set; }

    public string? FullyQualifiedDomainName { get; set; }

    public CustomDomainIdentity? Identity { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CustomDomainValidationState? ValidationState { get; set; }
}

internal sealed class CustomDomainIdentity
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CustomDomainIdentityType? Type { get; set; }

    public string? UserAssignedIdentity { get; set; }
}

internal sealed class RoutingEnrichments
{
    public List<DynamicRoutingEnrichment>? Dynamic { get; set; }

    public List<StaticStringRoutingEnrichment>? Static { get; set; }
}

internal sealed class DynamicRoutingEnrichment
{
    public string? Key { get; set; }

    public string? Value { get; set; }
}

internal sealed class StaticStringRoutingEnrichment
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    public string ValueType { get; set; } = "String";
}

internal sealed class RoutingIdentityInfo
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoutingIdentityType? Type { get; set; }

    public string? UserAssignedIdentity { get; set; }
}

internal sealed class NamespacePrivateEndpointConnection
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public NamespacePrivateEndpointConnectionProperties? Properties { get; set; }
}

internal sealed class NamespacePrivateEndpointConnectionProperties
{
    public List<string>? GroupIds { get; set; }
    public NamespacePrivateEndpoint? PrivateEndpoint { get; set; }
    public ConnectionState? PrivateLinkServiceConnectionState { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResourceProvisioningState? ProvisioningState { get; set; }
}

internal sealed class NamespacePrivateEndpoint
{
    public string? Id { get; set; }
}

internal sealed class ConnectionState
{
    public string? ActionsRequired { get; set; }
    public string? Description { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PersistedConnectionStatus? Status { get; set; }
}