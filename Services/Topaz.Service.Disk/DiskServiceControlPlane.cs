using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.Disk.Models;
using Topaz.Service.Disk.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Disk;

internal sealed class DiskServiceControlPlane(
    Pipeline eventPipeline,
    DiskResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string DiskNotFoundCode = "ResourceNotFound";
    private const string DiskNotFoundMessageTemplate = "Disk '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static DiskServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new DiskResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var disk = resource.As<DiskResource, DiskResourceProperties>();
        if (disk == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Disk instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(disk.Location))
        {
            logger.LogError($"Disk resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                disk.GetSubscription(),
                disk.GetResourceGroup(),
                disk.Name,
                CreateOrUpdateDiskRequest.FromResource(disk));

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<DiskResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string diskName)
    {
        var resource = provider.GetAs<DiskResource>(subscriptionIdentifier, resourceGroupIdentifier, diskName);

        return resource == null
            ? new ControlPlaneOperationResult<DiskResource>(
                OperationResult.NotFound,
                null,
                string.Format(DiskNotFoundMessageTemplate, diskName),
                DiskNotFoundCode)
            : new ControlPlaneOperationResult<DiskResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<DiskResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string diskName,
        CreateOrUpdateDiskRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<DiskResource>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<DiskResource>(subscriptionIdentifier, resourceGroupIdentifier, diskName);

        if (existing != null)
        {
            existing.Location = request.Location?.ToString() ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            DiskResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, diskName, existing);

            return new ControlPlaneOperationResult<DiskResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? resourceGroupOperation.Resource!.Location!;
        var sku = request.Sku == null
            ? null
            : new ResourceSku { Name = request.Sku.Name };
        var properties = DiskResourceProperties.FromRequest(request);
        var resource = new DiskResource(
            subscriptionIdentifier,
            resourceGroupIdentifier,
            diskName,
            new AzureLocation(location),
            request.Tags,
            sku,
            properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, diskName, resource,
            createOperation: true);

        return new ControlPlaneOperationResult<DiskResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string diskName)
    {
        var resource = provider.GetAs<DiskResource>(subscriptionIdentifier, resourceGroupIdentifier, diskName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(DiskNotFoundMessageTemplate, diskName),
                DiskNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, diskName);

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<DiskResource> Update(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string diskName,
        UpdateDiskRequest request)
    {
        var existing = provider.GetAs<DiskResource>(subscriptionIdentifier, resourceGroupIdentifier, diskName);

        if (existing == null)
        {
            return new ControlPlaneOperationResult<DiskResource>(
                OperationResult.NotFound,
                null,
                string.Format(DiskNotFoundMessageTemplate, diskName),
                DiskNotFoundCode);
        }

        if (request.Tags != null)
            existing.Tags = request.Tags;

        if (request.Properties?.DiskSizeGB.HasValue == true)
            existing.Properties.DiskSizeGB = request.Properties.DiskSizeGB;

        if (request.Sku?.Name != null)
        {
            var updatedSku = new ResourceSku { Name = request.Sku.Name };
            var updated = new DiskResource(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                diskName,
                new AzureLocation(existing.Location!),
                existing.Tags,
                updatedSku,
                existing.Properties);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, diskName, updated);
            return new ControlPlaneOperationResult<DiskResource>(OperationResult.Updated, updated, null, null);
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, diskName, existing);

        return new ControlPlaneOperationResult<DiskResource>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult<DiskResource[]> ListByResourceGroup(        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<DiskResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<DiskResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<DiskResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<DiskResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<DiskResource[]>(OperationResult.Success, resources, null, null);
    }
}
