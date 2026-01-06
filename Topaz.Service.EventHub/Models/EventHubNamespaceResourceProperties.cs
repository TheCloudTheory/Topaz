using JetBrains.Annotations;
using Topaz.Service.EventHub.Models.Requests;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubNamespaceResourceProperties
{
    public string? AlternateName { get; set; }
    public string? ClusterArmId { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public bool? DisableLocalAuth { get; set; }
    public EncryptionProperties? Encryption { get; set; }
    public bool? IsAutoInflateEnabled { get; set; }
    public bool? KafkaEnabled { get; set; }
    public int? MaximumThroughputUnits { get; set; }
    public string? MetricId { get; set; }
    public string? MinimumTlsVersion { get; set; }
    public List<PrivateEndpointConnection>? PrivateEndpointConnections { get; set; }
    public string? ProvisioningState { get; set; }
    public string? PublicNetworkAccess { get; set; }
    public string? ServiceBusEndpoint { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool? ZoneRedundant { get; set; }

    public static EventHubNamespaceResourceProperties From(CreateOrUpdateEventHubNamespaceRequest request)
    {
        return new EventHubNamespaceResourceProperties
        {
            DisableLocalAuth = request.Properties?.DisableLocalAuth,
            IsAutoInflateEnabled = request.Properties?.IsAutoInflateEnabled,
            KafkaEnabled = request.Properties?.KafkaEnabled,
            MaximumThroughputUnits = request.Properties?.MaximumThroughputUnits,
            MinimumTlsVersion = request.Properties?.MinimumTlsVersion,
            PublicNetworkAccess = request.Properties?.PublicNetworkAccess,
            ZoneRedundant = request.Properties?.ZoneRedundant
        };
    }

    [UsedImplicitly]
    internal sealed class EncryptionProperties
    {
        public string? KeySource { get; set; }
        public List<KeyVaultEncryptionProperties>? KeyVaultProperties { get; set; }
        public bool? RequireInfrastructureEncryption { get; set; }
    }

    [UsedImplicitly]
    internal sealed class KeyVaultEncryptionProperties
    {
        public KeyVaultIdentity? Identity { get; set; }
        public string? KeyName { get; set; }
        public string? KeyVaultUri { get; set; }
        public string? KeyVersion { get; set; }
    }

    [UsedImplicitly]
    internal sealed class KeyVaultIdentity
    {
        public string? UserAssignedIdentity { get; set; }
    }

    [UsedImplicitly]
    internal sealed class PrivateEndpointConnection
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Location { get; set; }
        public PrivateEndpointConnectionProperties? Properties { get; set; }
        public SystemData? SystemData { get; set; }
    }

    [UsedImplicitly]
    internal sealed class PrivateEndpointConnectionProperties
    {
        public PrivateEndpoint? PrivateEndpoint { get; set; }
        public ConnectionState? PrivateLinkServiceConnectionState { get; set; }
        public string? ProvisioningState { get; set; }
    }

    [UsedImplicitly]
    internal sealed class PrivateEndpoint
    {
        public string? Id { get; set; }
    }

    [UsedImplicitly]
    internal sealed class ConnectionState
    {
        public string? Description { get; set; }
        public string? Status { get; set; }
    }

    [UsedImplicitly]
    internal sealed class SystemData
    {
        public DateTimeOffset? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedByType { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedByType { get; set; }
    }
}