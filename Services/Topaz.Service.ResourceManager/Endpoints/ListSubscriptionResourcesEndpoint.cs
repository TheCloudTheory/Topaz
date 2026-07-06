using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.AppConfiguration;
using Topaz.Service.AppService;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.CosmosDb;
using Topaz.Service.Disk;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.LoadBalancer;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ServiceBus;
using Topaz.Service.Sql;
using Topaz.Service.Storage;
using Topaz.Service.VirtualNetwork;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Service.VirtualMachine;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

/// <summary>
/// Handles generic subscription-scoped resource list requests filtered by resource type.
/// Corresponds to: GET /subscriptions/{subscriptionId}/resources?$filter=resourceType eq '...'
/// This is the single point of dispatch for all resource types; add new cases here as services
/// are onboarded rather than adding per-service endpoints.
/// </summary>
internal sealed class ListSubscriptionResourcesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _kvControlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly ContainerRegistryControlPlane _acrControlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
    private readonly VirtualMachineServiceControlPlane _vmControlPlane = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
    private readonly DiskServiceControlPlane _diskControlPlane = DiskServiceControlPlane.New(eventPipeline, logger);
    private readonly AppConfigurationServiceControlPlane _appConfigControlPlane = AppConfigurationServiceControlPlane.New(eventPipeline, logger);
    private readonly AppServicePlanControlPlane _appServicePlanControlPlane = AppServicePlanControlPlane.New(logger);
    private readonly AppServiceSiteControlPlane _appServiceSiteControlPlane = AppServiceSiteControlPlane.New(logger);
    private readonly CosmosDbServiceControlPlane _cosmosDbControlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);
    private readonly EventHubServiceControlPlane _eventHubControlPlane = EventHubServiceControlPlane.New(logger);
    private readonly LoadBalancerControlPlane _lbControlPlane = LoadBalancerControlPlane.New(eventPipeline, logger);
    private readonly ManagedIdentityControlPlane _managedIdentityControlPlane = ManagedIdentityControlPlane.New(eventPipeline, logger);
    private readonly ServiceBusServiceControlPlane _serviceBusControlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
    private readonly SqlServiceControlPlane _sqlControlPlane = SqlServiceControlPlane.New(eventPipeline, logger);
    private readonly AzureStorageControlPlane _storageControlPlane = AzureStorageControlPlane.New(logger);
    private readonly VirtualNetworkControlPlane _vnetControlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);
    private readonly NetworkInterfaceControlPlane _nicControlPlane = NetworkInterfaceControlPlane.New(eventPipeline, logger);
    private readonly PublicIpAddressControlPlane _pipControlPlane = PublicIpAddressControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => null;

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resources"
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        context.Request.Query.TryGetValueForKey("$filter", out var filter);
        var resourceType = ParseResourceType(filter);

        logger.LogDebug(nameof(ListSubscriptionResourcesEndpoint), nameof(GetResponse),
            "Listing subscription resources: filter=`{0}`, resourceType=`{1}`.", filter, resourceType);

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

            ListSubscriptionResourcesResponse.GenericResourceExpanded[] items = resourceType switch
            {
                "Microsoft.KeyVault/vaults" => Map(_kvControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.ContainerRegistry/registries" => Map(_acrControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Compute/virtualMachines" => Map(_vmControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Compute/disks" => Map(_diskControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.AppConfiguration/configurationStores" => Map(_appConfigControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Web/serverfarms" => Map(_appServicePlanControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Web/sites" => Map(_appServiceSiteControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.DocumentDB/databaseAccounts" => Map(_cosmosDbControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.EventHub/namespaces" => Map(_eventHubControlPlane.ListNamespacesBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Network/loadBalancers" => Map(_lbControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.ManagedIdentity/userAssignedIdentities" => Map(_managedIdentityControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.ServiceBus/namespaces" => Map(_serviceBusControlPlane.ListNamespacesBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Sql/servers" => Map(_sqlControlPlane.ListBySubscription(subscriptionIdentifier)),
                "Microsoft.Storage/storageAccounts" => Map(_storageControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Network/virtualNetworks" => Map(_vnetControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Network/networkInterfaces" => Map(_nicControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                "Microsoft.Network/publicIPAddresses" => Map(_pipControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? []),
                _ => []
            };

            response.CreateJsonContentResponse(new ListSubscriptionResourcesResponse { Value = items });
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    /// <summary>
    /// Parses the resource type from a $filter value such as:
    /// <c>resourceType eq 'Microsoft.KeyVault/vaults'</c>
    /// or a compound filter like:
    /// <c>resourceGroup eq 'my-rg' and resourceType eq 'Microsoft.KeyVault/vaults'</c>
    /// Returns the extracted type string, or null if the filter cannot be parsed.
    /// </summary>
    internal static string? ParseResourceType(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return null;

        // Search specifically for "resourceType eq '" to avoid accidentally picking up
        // other conditions in compound filters (e.g. "resourceGroup eq '...' and resourceType eq '...'").
        const string prefix = "resourceType eq '";
        var typeIndex = filter.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (typeIndex < 0) return null;

        var start = typeIndex + prefix.Length;
        var end = filter.IndexOf('\'', start);
        return end > start ? filter[start..end] : null;
    }

    internal static ListSubscriptionResourcesResponse.GenericResourceExpanded[] Map<T>(IEnumerable<ArmResource<T>> resources)
        => resources.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray();
}
