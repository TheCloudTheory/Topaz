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
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Sql;
using Topaz.Service.Storage;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Service.VirtualMachine;
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

/// <summary>
/// Handles generic resource-group-scoped resource list requests.
/// Corresponds to: GET /subscriptions/{subscriptionId}/resourceGroups/{rg}/resources[?$filter=resourceType eq '...']
/// This is the single point of dispatch for all resource types; add new cases here as services
/// are onboarded rather than adding per-service endpoints.
/// </summary>
internal sealed class ListResourceGroupResourcesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppConfigurationServiceControlPlane _appConfigControlPlane = AppConfigurationServiceControlPlane.New(eventPipeline, logger);
    private readonly AppServicePlanControlPlane _appServicePlanControlPlane = AppServicePlanControlPlane.New(logger);
    private readonly AppServiceSiteControlPlane _appServiceSiteControlPlane = AppServiceSiteControlPlane.New(logger);
    private readonly ContainerRegistryControlPlane _acrControlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
    private readonly CosmosDbServiceControlPlane _cosmosDbControlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);
    private readonly DiskServiceControlPlane _diskControlPlane = DiskServiceControlPlane.New(eventPipeline, logger);
    private readonly EventHubServiceControlPlane _eventHubControlPlane = EventHubServiceControlPlane.New(logger);
    private readonly KeyVaultControlPlane _kvControlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly LoadBalancerControlPlane _lbControlPlane = LoadBalancerControlPlane.New(eventPipeline, logger);
    private readonly ManagedIdentityControlPlane _managedIdentityControlPlane = ManagedIdentityControlPlane.New(eventPipeline, logger);
    private readonly NetworkInterfaceControlPlane _nicControlPlane = NetworkInterfaceControlPlane.New(eventPipeline, logger);
    private readonly PrivateEndpointControlPlane _peControlPlane = PrivateEndpointControlPlane.New(eventPipeline, logger);
    private readonly PublicIpAddressControlPlane _pipControlPlane = PublicIpAddressControlPlane.New(eventPipeline, logger);
    private readonly ServiceBusServiceControlPlane _serviceBusControlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
    private readonly SqlServiceControlPlane _sqlControlPlane = SqlServiceControlPlane.New(eventPipeline, logger);
    private readonly AzureStorageControlPlane _storageControlPlane = AzureStorageControlPlane.New(logger);
    private readonly VirtualMachineServiceControlPlane _vmControlPlane = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
    private readonly VirtualNetworkControlPlane _vnetControlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => null;

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/resources"
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        context.Request.Query.TryGetValueForKey("$filter", out var filter);
        var resourceType = ListSubscriptionResourcesEndpoint.ParseResourceType(filter);

        logger.LogDebug(nameof(ListResourceGroupResourcesEndpoint), nameof(GetResponse),
            "Listing resource group resources: filter=`{0}`, resourceType=`{1}`.", filter, resourceType);

        try
        {
            var path = context.Request.Path.Value!;
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));

            var items = ListAll(subscriptionIdentifier, resourceGroupIdentifier, resourceType);

            response.CreateJsonContentResponse(new ListSubscriptionResourcesResponse { Value = items });
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private ListSubscriptionResourcesResponse.GenericResourceExpanded[] ListAll(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string? resourceType)
    {
        var results = new List<ListSubscriptionResourcesResponse.GenericResourceExpanded>();

        void Add<T>(IEnumerable<ArmResource<T>>? items)
        {
            if (items == null) return;
            results.AddRange(items.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!));
        }

        if (resourceType == null || resourceType == "Microsoft.AppConfiguration/configurationStores")
            Add(_appConfigControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Web/serverfarms")
            Add(_appServicePlanControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Web/sites")
            Add(_appServiceSiteControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.ContainerRegistry/registries")
            Add(_acrControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.DocumentDB/databaseAccounts")
            Add(_cosmosDbControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Compute/disks")
            Add(_diskControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.EventHub/namespaces")
            Add(_eventHubControlPlane.ListNamespaces(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.KeyVault/vaults")
            Add((_kvControlPlane.ListBySubscription(sub).Resource ?? []).Where(r => r.IsInResourceGroup(rg)));
        if (resourceType == null || resourceType == "Microsoft.Network/loadBalancers")
            Add(_lbControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.ManagedIdentity/userAssignedIdentities")
            Add(_managedIdentityControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Network/networkInterfaces")
            Add(_nicControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Network/privateEndpoints")
            Add(_peControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Network/publicIPAddresses")
            Add(_pipControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.ServiceBus/namespaces")
            Add(_serviceBusControlPlane.ListNamespaces(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Sql/servers")
            Add(_sqlControlPlane.ListByResourceGroup(sub, rg));
        if (resourceType == null || resourceType == "Microsoft.Storage/storageAccounts")
            Add(_storageControlPlane.List(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Compute/virtualMachines")
            Add(_vmControlPlane.ListByResourceGroup(sub, rg).Resource);
        if (resourceType == null || resourceType == "Microsoft.Network/virtualNetworks")
            Add(_vnetControlPlane.ListByResourceGroup(sub, rg).Resource);

        return [.. results];
    }
}

