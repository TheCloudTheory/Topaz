using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubNamespaceResource
    : ArmResource<EventHubNamespaceResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public EventHubNamespaceResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }
    
    public EventHubNamespaceResource(SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        AzureLocation location,
        EventHubNamespaceIdentifier identifier,
        EventHubNamespaceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.EventHub/namespaces/{identifier}";
        Name = identifier.Value;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.EventHub/namespaces";
    public override string Location { get; set; }
    public override IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override EventHubNamespaceResourceProperties Properties { get; init; }

    public void UpdateProperties(EventHubNamespaceResourceProperties properties)
    {
        Properties.AlternateName = properties.AlternateName;
        Properties.ClusterArmId = properties.ClusterArmId;
        Properties.DisableLocalAuth = properties.DisableLocalAuth;
        Properties.Encryption = properties.Encryption;
        Properties.IsAutoInflateEnabled = properties.IsAutoInflateEnabled;
        Properties.KafkaEnabled = properties.KafkaEnabled;
        Properties.MaximumThroughputUnits = properties.MaximumThroughputUnits;
        Properties.MetricId = properties.MetricId;
        Properties.MinimumTlsVersion = properties.MinimumTlsVersion;
        Properties.PrivateEndpointConnections = properties.PrivateEndpointConnections;
        Properties.PublicNetworkAccess = properties.PublicNetworkAccess;
        Properties.ServiceBusEndpoint = properties.ServiceBusEndpoint;
        Properties.ZoneRedundant = properties.ZoneRedundant;
    }
}