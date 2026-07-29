using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Service.VirtualMachine.Models;
using Topaz.Service.VirtualMachine.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

internal sealed class AvailabilitySetControlPlane(Pipeline eventPipeline, AvailabilitySetResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    public static AvailabilitySetControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(eventPipeline, new AvailabilitySetResourceProvider(logger), logger);
    
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);
    
    public OperationResult Deploy(GenericResource resource)
    {
        var availabilitySet = resource.As<AvailabilitySetResource, AvailabilitySetResourceProperties>();
        if (availabilitySet == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Virtual Machine instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(availabilitySet.Location))
        {
            logger.LogError($"Virtual machine resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                availabilitySet.GetSubscription(),
                availabilitySet.GetResourceGroup(),
                availabilitySet.Name,
                new CreateOrUpdateAvailabilitySetRequest
                {
                    Location = availabilitySet.Location,
                    PlatformFaultDomainCount = availabilitySet.Properties.PlatformFaultDomainCount,
                    PlatformUpdateDomainCount = availabilitySet.Properties.PlatformUpdateDomainCount,
                    ProximityPlacementGroup = new SubResource
                    {
                        Id = availabilitySet.Properties.ProximityPlacementGroup?.Id
                    },
                    ScheduledEventsPolicy = availabilitySet.Properties.ScheduledEventsPolicy,
                    Sku = VirtualMachineScaleSetSku.From(availabilitySet.Sku),
                    VirtualMachines = availabilitySet.Properties.VirtualMachines
                });

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<AvailabilitySetResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string availabilitySetName, CreateOrUpdateAvailabilitySetRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AvailabilitySetResource>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing =
            provider.GetAs<AvailabilitySetResource>(subscriptionIdentifier, resourceGroupIdentifier,
                availabilitySetName);
        if (existing != null)
        {
            existing.Properties.UpdateFromRequest(request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName, existing);

            return new ControlPlaneOperationResult<AvailabilitySetResource>(
                OperationResult.Updated,
                existing);
        }

        var availabilitySet = new AvailabilitySetResource(subscriptionIdentifier, resourceGroupIdentifier,
            availabilitySetName, request.Location!, null, AvailabilitySetResourceProperties.From(request));
        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName, availabilitySet);

        return new ControlPlaneOperationResult<AvailabilitySetResource>(
            OperationResult.Created,
            availabilitySet);
    }
}