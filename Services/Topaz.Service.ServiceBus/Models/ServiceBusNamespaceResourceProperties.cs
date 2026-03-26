using Topaz.ResourceManager;
using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusNamespaceResourceProperties
{
    public ResourceSku? Sku { get; init; }
    public string? Location { get; init; }
    public object? Identity { get; init; }
    public object? MinimumTlsVersion { get; init; }
    public string? ProvisioningState => "Succeeded";
    public string? Status { get; init; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? ServiceBusEndpoint { get; init; }
    public string? MetricId { get; init; }
    public bool? IsZoneRedundant { get; init; }
    public object? Encryption { get; init; }
    public IList<object>? PrivateEndpointConnections { get; init; }
    public bool? DisableLocalAuth { get; init; }
    public string? AlternateName { get; init; }
    public object? PublicNetworkAccess { get; init; }
    public int? PremiumMessagingPartitions { get; init; }

    public static ServiceBusNamespaceResourceProperties From(CreateOrUpdateServiceBusNamespaceRequest request)
    {
        return new ServiceBusNamespaceResourceProperties
        {
            Sku = request.Sku,
            Location = request.Location,
            Identity = request.Properties!.Identity,
            MinimumTlsVersion = request.Properties!.MinimumTlsVersion,
            ServiceBusEndpoint = request.Properties!.ServiceBusEndpoint,
            MetricId = request.Properties!.MetricId,
            IsZoneRedundant = request.Properties!.IsZoneRedundant,
            Encryption = request.Properties!.Encryption,
            PrivateEndpointConnections = request.Properties!.PrivateEndpointConnections,
            DisableLocalAuth = request.Properties!.DisableLocalAuth,
            AlternateName = request.Properties!.AlternateName,
            PublicNetworkAccess = request.Properties!.PublicNetworkAccess,
            PremiumMessagingPartitions = request.Properties!.PremiumMessagingPartitions
        };
    }
}