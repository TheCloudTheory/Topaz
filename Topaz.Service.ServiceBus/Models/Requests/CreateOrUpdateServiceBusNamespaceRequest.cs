using JetBrains.Annotations;
using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

internal sealed class CreateOrUpdateServiceBusNamespaceRequest
{
    public ResourceSku? Sku { get; init; }
    public string? Location { get; init; }
    public CreateOrUpdateServiceBusNamespaceRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    internal class CreateOrUpdateServiceBusNamespaceRequestProperties
    {
        public object? Identity { get; init; }
        public object? MinimumTlsVersion { get; init; }
        public string? ServiceBusEndpoint { get; init; }
        public string? MetricId { get; init; }
        public bool? IsZoneRedundant { get; init; }
        public object? Encryption { get; init; }
        public IList<object>? PrivateEndpointConnections { get; init; }
        public bool? DisableLocalAuth { get; init; }
        public string? AlternateName { get; init; }
        public object? PublicNetworkAccess { get; init; }
        public int? PremiumMessagingPartitions { get; init; }
    }
}