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
using Topaz.Service.ResourceGroup;
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
    private readonly ResourceGroupControlPlane _rgControlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);

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

            var items = resourceType is not null
                ? ListByType(subscriptionIdentifier, resourceType)
                : ListAll(subscriptionIdentifier);

            response.CreateJsonContentResponse(new ListSubscriptionResourcesResponse { Value = items });
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private ListSubscriptionResourcesResponse.GenericResourceExpanded[] ListByType(
        SubscriptionIdentifier sub, string resourceType) => resourceType switch
    {
        "Microsoft.KeyVault/vaults" => Map(_kvControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.ContainerRegistry/registries" => Map(_acrControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Compute/virtualMachines" => Map(_vmControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Compute/disks" => Map(_diskControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.AppConfiguration/configurationStores" => Map(_appConfigControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Web/serverfarms" => Map(_appServicePlanControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Web/sites" => Map(_appServiceSiteControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.DocumentDB/databaseAccounts" => Map(_cosmosDbControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.EventHub/namespaces" => Map(_eventHubControlPlane.ListNamespacesBySubscription(sub).Resource ?? []),
        "Microsoft.Network/loadBalancers" => Map(_lbControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.ManagedIdentity/userAssignedIdentities" => Map(_managedIdentityControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.ServiceBus/namespaces" => Map(_serviceBusControlPlane.ListNamespacesBySubscription(sub).Resource ?? []),
        "Microsoft.Sql/servers" => Map(_sqlControlPlane.ListBySubscription(sub)),
        "Microsoft.Storage/storageAccounts" => Map(_storageControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Network/virtualNetworks" => Map(_vnetControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Network/networkInterfaces" => Map(_nicControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Network/publicIPAddresses" => Map(_pipControlPlane.ListBySubscription(sub).Resource ?? []),
        "Microsoft.Resources/resourceGroups" => Map(_rgControlPlane.List(sub).resources),
        _ => []
    };

    private ListSubscriptionResourcesResponse.GenericResourceExpanded[] ListAll(SubscriptionIdentifier sub)
    {
        var results = new List<ListSubscriptionResourcesResponse.GenericResourceExpanded>();
        results.AddRange(Map(_kvControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_acrControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_vmControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_diskControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_appConfigControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_appServicePlanControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_appServiceSiteControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_cosmosDbControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_eventHubControlPlane.ListNamespacesBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_lbControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_managedIdentityControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_serviceBusControlPlane.ListNamespacesBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_sqlControlPlane.ListBySubscription(sub)));
        results.AddRange(Map(_storageControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_vnetControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_nicControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_pipControlPlane.ListBySubscription(sub).Resource ?? []));
        results.AddRange(Map(_rgControlPlane.List(sub).resources));
        return [.. results];
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
