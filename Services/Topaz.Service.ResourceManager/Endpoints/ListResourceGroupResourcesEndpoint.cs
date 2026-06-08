using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.Disk;
using Topaz.Service.KeyVault;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Service.VirtualMachine;
using Topaz.Shared;
using Topaz.Shared.Extensions;namespace Topaz.Service.ResourceManager.Endpoints;

/// <summary>
/// Handles generic resource-group-scoped resource list requests filtered by resource type.
/// Corresponds to: GET /subscriptions/{subscriptionId}/resourceGroups/{rg}/resources?$filter=resourceType eq '...'
/// This is the single point of dispatch for all resource types; add new cases here as services
/// are onboarded rather than adding per-service endpoints.
/// </summary>
internal sealed class ListResourceGroupResourcesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _kvControlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly ContainerRegistryControlPlane _acrControlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
    private readonly VirtualMachineServiceControlPlane _vmControlPlane = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
    private readonly DiskServiceControlPlane _diskControlPlane = DiskServiceControlPlane.New(eventPipeline, logger);

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

            ListSubscriptionResourcesResponse.GenericResourceExpanded[] items = resourceType switch
            {
                "Microsoft.KeyVault/vaults" => Map((_kvControlPlane.ListBySubscription(subscriptionIdentifier).Resource ?? [])
                    .Where(r => r.IsInResourceGroup(resourceGroupIdentifier))),
                "Microsoft.ContainerRegistry/registries" => Map(_acrControlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier).Resource ?? []),
                "Microsoft.Compute/virtualMachines" => Map(_vmControlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier).Resource ?? []),
                "Microsoft.Compute/disks" => Map(_diskControlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier).Resource ?? []),
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

    private static ListSubscriptionResourcesResponse.GenericResourceExpanded[] Map<T>(IEnumerable<ArmResource<T>> resources)
        => resources.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray();
}
