using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.AppConfiguration;
using Topaz.Service.AppConfiguration.Models;
using Topaz.Service.AppService;
using Topaz.Service.AppService.Models.Requests;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.CosmosDb;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Disk;
using Topaz.Service.Disk.Models.Requests;
using Topaz.Service.EventHub;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.Insights;
using Topaz.Service.Insights.Models;
using Topaz.Service.KeyVault;
using Topaz.Service.KeyVault.Models.Requests.Vault;
using Topaz.Service.LoadBalancer;
using Topaz.Service.LoadBalancer.Models.Requests;
using Topaz.Service.LogAnalytics;
using Topaz.Service.LogAnalytics.Models;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ManagedIdentity.Models.Requests;
using Topaz.Service.Redis;
using Topaz.Service.Redis.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.ServiceBus;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Sql;
using Topaz.Service.Sql.Models.Requests;
using Topaz.Service.Storage;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Subscription;
using Topaz.Service.VirtualMachine;
using Topaz.Service.VirtualMachine.Models.Requests;
using Topaz.Service.VirtualNetwork;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;
using CreateOrUpdateSqlDatabaseRequest = Topaz.Service.Sql.Models.Requests.CreateOrUpdateSqlDatabaseRequest;

namespace Topaz.Importer;

public class AzureImporterService(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        ResourceGroupControlPlane.New(eventPipeline, logger);

    public async Task<ImportResult> Import(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier,
        string? resourceType, bool dryRun, bool overwrite)
    {
        if (resourceGroupIdentifier != null)
            return await RunResourceGroupScopedImport(subscriptionIdentifier, resourceGroupIdentifier!, resourceType,
                dryRun, overwrite);
        
        logger.LogInformation(
            $"Running subscription scoped import for sub {subscriptionIdentifier.Value} and resource type {resourceType}");
        
        return await RunSubscriptionScopedImport(subscriptionIdentifier, resourceType, dryRun, overwrite);

    }

    private async Task<ImportResult> RunResourceGroupScopedImport(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string? resourceType, bool dryRun, bool overwrite)
    {
        logger.LogDebug(nameof(AzureImporterService), nameof(RunSubscriptionScopedImport),
            "Running resource group scoped import for sub {0} and resource type {1} and resource group {2}", subscriptionIdentifier.Value,
            resourceType, resourceGroupIdentifier?.Value);
        
        var importResult = new ImportResult(dryRun);
        var armClient = new ArmClient(new DefaultAzureCredential(), subscriptionIdentifier.Value.ToString());
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // Only create the subscription if it doesn't already exist
        if (_subscriptionControlPlane.Get(subscriptionIdentifier).Result == OperationResult.NotFound && !dryRun)
        {
            importResult.AddSubscription(subscriptionIdentifier);
            _subscriptionControlPlane.Create(subscriptionIdentifier, subscription.Data.DisplayName,
                subscription.Data.Tags.ToDictionary());
        }
        
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupIdentifier?.Value);
        importResult.AddResourceGroup(resourceGroupIdentifier!);
        
        await ProcessResourceImport(subscriptionIdentifier, resourceType, resourceGroup, importResult, resourceGroup.Value.GetGenericResourcesAsync(), dryRun, overwrite);

        return importResult;
    }

    private async Task<ImportResult> RunSubscriptionScopedImport(SubscriptionIdentifier subscriptionIdentifier,
        string? resourceType, bool dryRun, bool overwrite)
    {
        logger.LogDebug(nameof(AzureImporterService), nameof(RunSubscriptionScopedImport),
            "Running subscription scoped import for sub {0} and resource type {1}", subscriptionIdentifier.Value,
            resourceType);

        var importResult = new ImportResult(dryRun);
        var armClient = new ArmClient(new DefaultAzureCredential(), subscriptionIdentifier.Value.ToString());
        var mgs = await armClient.GetManagementGroups().GetAllAsync().ToArrayAsync();
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // Only create the subscription if it doesn't already exist
        if (_subscriptionControlPlane.Get(subscriptionIdentifier).Result == OperationResult.NotFound && !dryRun)
        {
            importResult.AddSubscription(subscriptionIdentifier);
            _subscriptionControlPlane.Create(subscriptionIdentifier, subscription.Data.DisplayName,
                subscription.Data.Tags.ToDictionary());
        }

        var resourceGroups = subscription.GetResourceGroups().ToArray();

        logger.LogDebug(nameof(AzureImporterService), nameof(RunSubscriptionScopedImport),
            "Found {0} management groups and {1} resource groups", mgs.Length, resourceGroups.Length);

        foreach (var rg in resourceGroups)
        {
            ImportResourceGroup(subscriptionIdentifier, rg, importResult, dryRun, overwrite);
            await ProcessResourceImport(subscriptionIdentifier, resourceType, rg, importResult, rg.GetGenericResourcesAsync(), dryRun, overwrite);
        }

        return new ImportResult(dryRun);
    }

    private async Task ProcessResourceImport(SubscriptionIdentifier subscriptionIdentifier, string? resourceType,
        ResourceGroupResource rg, ImportResult importResult, AsyncPageable<GenericResource> resources, bool dryRun, bool overwrite)
    {
        await foreach (var resource in resources)
        {
            logger.LogDebug(nameof(AzureImporterService), nameof(RunSubscriptionScopedImport), "Found resource {0}",
                resource.Id);

            var rgId = ResourceGroupIdentifier.From(rg.Data.Name);
            switch (resource.Data.ResourceType.Type.ToLowerInvariant())
            {
                case "microsoft.containerregistry/registries":
                {
                    var cp = ContainerRegistryControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateContainerRegistryRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.keyvault/vaults":
                {
                    var cp = KeyVaultControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateKeyVaultRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.network/virtualnetworks":
                {
                    var cp = VirtualNetworkControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateVirtualNetworkRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.network/networksecuritygroups":
                {
                    var cp = NetworkSecurityGroupControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateNetworkSecurityGroupRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.network/networkinterfaces":
                {
                    var cp = NetworkInterfaceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateNetworkInterfaceRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.network/privateendpoints":
                {
                    var cp = PrivateEndpointControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdatePrivateEndpointRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.network/publicipaddresses":
                {
                    var cp = PublicIpAddressControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdatePublicIpAddressRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.network/loadbalancers":
                {
                    var cp = LoadBalancerControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateLoadBalancerRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.compute/virtualmachines":
                {
                    var cp = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateVirtualMachineRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.compute/disks":
                {
                    var cp = DiskServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateDiskRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.appconfiguration/configurationstores":
                {
                    var cp = AppConfigurationServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            ConfigurationStoreResource.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.operationalinsights/workspaces":
                {
                    var cp = LogAnalyticsServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            WorkspaceResource.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.insights/components":
                {
                    var cp = ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            ApplicationInsightsComponentResource.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.managedidentity/userassignedidentities":
                {
                    var cp = ManagedIdentityControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, ManagedIdentityIdentifier.From(resource.Data.Name)).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, ManagedIdentityIdentifier.From(resource.Data.Name),
                            CreateUpdateManagedIdentityRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.eventhub/namespaces":
                {
                    var cp = EventHubServiceControlPlane.New(logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.GetNamespace(subscriptionIdentifier, rgId, EventHubNamespaceIdentifier.From(resource.Data.Name)).Result,
                        () => cp.CreateOrUpdateNamespace(subscriptionIdentifier, rgId, resource.Data.Location, EventHubNamespaceIdentifier.From(resource.Data.Name),
                            CreateOrUpdateEventHubNamespaceRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.servicebus/namespaces":
                {
                    var cp = ServiceBusServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.GetNamespace(subscriptionIdentifier, rgId, ServiceBusNamespaceIdentifier.From(resource.Data.Name)).Result,
                        () => cp.CreateOrUpdateNamespace(subscriptionIdentifier, rgId, resource.Data.Location, ServiceBusNamespaceIdentifier.From(resource.Data.Name),
                            CreateOrUpdateServiceBusNamespaceRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.storage/storageaccounts":
                {
                    var cp = AzureStorageControlPlane.New(logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateStorageAccountRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.web/serverfarms":
                {
                    var cp = AppServicePlanControlPlane.New(logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateAppServicePlanRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.web/sites":
                {
                    var cp = AppServiceSiteControlPlane.New(logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateAppServiceSiteRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.sql/servers":
                {
                    var cp = SqlServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateSqlServerRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.sql/servers/databases":
                {
                    var cp = SqlServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdateDatabase(subscriptionIdentifier, rgId, resource.Data.Id.Parent!.Name, resource.Data.Name,
                            CreateOrUpdateSqlDatabaseRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.documentdb/databaseaccounts":
                {
                    var cp = CosmosDbServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateDatabaseAccountRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.cache/redis":
                {
                    var cp = RedisServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            RedisResource.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.compute/availabilitysets":
                {
                    var cp = AvailabilitySetControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateAvailabilitySetRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.apimanagement/service":
                {
                    var cp = ApiManagementServiceControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Data.Name).Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId, resource.Data.Name,
                            CreateOrUpdateApiManagementServiceRequest.From(resource.Data)).Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.apimanagement/service/apis":
                {
                    var cp = ApiManagementApiControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Id.Parent!.Name, resource.Data.Name)
                            .Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId,
                            resource.Id.Parent!.Name, resource.Data.Name,
                            CreateOrUpdateApiRequest.From(resource.Data), "*").Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.apimanagement/service/backends":
                {
                    var cp = ApiManagementBackendControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Id.Parent!.Name, resource.Data.Name)
                            .Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId,
                            resource.Id.Parent!.Name, resource.Data.Name,
                            CreateOrUpdateBackendRequest.From(resource.Data), "*").Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.apimanagement/service/products":
                {
                    var cp = ApiManagementProductControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Id.Parent!.Name, resource.Data.Name)
                            .Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId,
                            resource.Id.Parent!.Name, resource.Data.Name,
                            CreateOrUpdateProductRequest.From(resource.Data), "*").Result, dryRun, overwrite);
                    break;
                }
                case "microsoft.apimanagement/service/policies":
                {
                    var cp = ApiManagementPolicyControlPlane.New(eventPipeline, logger);
                    TryImportResource(resource.Data.Id, importResult,
                        () => cp.Get(subscriptionIdentifier, rgId, resource.Id.Parent!.Name, resource.Data.Name)
                            .Result,
                        () => cp.CreateOrUpdate(subscriptionIdentifier, rgId,
                            resource.Id.Parent!.Name, resource.Data.Name,
                            CreateOrUpdatePolicyRequest.From(resource.Data), "*").Result, dryRun, overwrite);
                    break;
                }
                default:
                    logger.LogWarning($"Deployment of resource type {resourceType} is not yet supported.");
                    break;
            }
        }
    }

    private void TryImportResource(ResourceIdentifier resourceId, ImportResult importResult,
        Func<OperationResult> get, Func<OperationResult> createOrUpdate, bool dryRun, bool overwrite)
    {
        if (dryRun) return;
        var existing = get();
        if (existing == OperationResult.NotFound || (existing == OperationResult.Success && overwrite))
        {
            if (createOrUpdate() is OperationResult.Created or OperationResult.Updated)
                importResult.Add(resourceId);
        }
    }

    private void ImportResourceGroup(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupResource rg,
        ImportResult importResult, bool dryRun, bool overwrite)
    {
        logger.LogDebug(nameof(AzureImporterService), nameof(RunSubscriptionScopedImport), "Found resource group {0}",
            rg.Id);

        var resourceGroupIdentifier = ResourceGroupIdentifier.From(rg.Data.Name);
        if (!dryRun && _resourceGroupControlPlane
                .Get(subscriptionIdentifier, resourceGroupIdentifier).Result == OperationResult.Success && overwrite)
        {
            _resourceGroupControlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier,
                CreateOrUpdateResourceGroupRequest.From(rg.Data));
            importResult.AddResourceGroup(resourceGroupIdentifier);
        }

        if (dryRun || _resourceGroupControlPlane
                .Get(subscriptionIdentifier, resourceGroupIdentifier).Result !=
            OperationResult.NotFound) return;
        
        _resourceGroupControlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier,
            CreateOrUpdateResourceGroupRequest.From(rg.Data));
        importResult.AddResourceGroup(resourceGroupIdentifier);
    }
}