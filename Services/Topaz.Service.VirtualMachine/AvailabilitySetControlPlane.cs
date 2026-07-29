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
    
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    private readonly VirtualMachineServiceControlPlane _virtualMachineControlPlane =
        VirtualMachineServiceControlPlane.New(eventPipeline, logger);
    
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
            Get(subscriptionIdentifier, resourceGroupIdentifier,
                availabilitySetName);
        if (existing.Result != OperationResult.NotFound)
        {
            existing.Resource!.UpdateFromRequest(request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName, existing);

            return new ControlPlaneOperationResult<AvailabilitySetResource>(
                OperationResult.Updated,
                existing.Resource);
        }

        var availabilitySet = new AvailabilitySetResource(subscriptionIdentifier, resourceGroupIdentifier,
            availabilitySetName, request.Location!, null, AvailabilitySetResourceProperties.From(request));
        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName, availabilitySet);

        return new ControlPlaneOperationResult<AvailabilitySetResource>(
            OperationResult.Created,
            availabilitySet);
    }

    public ControlPlaneOperationResult<AvailabilitySetResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string availabilitySetName)
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

        if (existing == null)
        {
            return new ControlPlaneOperationResult<AvailabilitySetResource>(OperationResult.NotFound, null,
                $"Availability set '{availabilitySetName}' not found.", "AvailabilitySetNotFound");
        }

        return new ControlPlaneOperationResult<AvailabilitySetResource>(OperationResult.Success, existing);
    }

    public ControlPlaneOperationResult Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string availabilitySetName)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, 
                $"Availability set '{availabilitySetName}' not found.", "AvailabilitySetNotFound");
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<AvailabilitySetResource[]> List(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AvailabilitySetResource[]>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }
        
        var existing = provider.ListAs<AvailabilitySetResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        return new ControlPlaneOperationResult<AvailabilitySetResource[]>(OperationResult.Success, [.. existing]);
    }

    public ControlPlaneOperationResult<VirtualMachineSize[]> ListAvailableSizes(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string availabilitySetName)
    {
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<VirtualMachineSize[]>(OperationResult.NotFound, null);
        }

        var allSizes = ComputeResourceSkuProvider.GetVirtualMachineSizes();

        if (existing.Resource!.Properties.VirtualMachines == null || existing.Resource!.Properties.VirtualMachines.Length == 0)
        {
            return new ControlPlaneOperationResult<VirtualMachineSize[]>(OperationResult.Success, allSizes);
        }

        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var vmRef in existing.Resource.Properties.VirtualMachines)
        {
            if (vmRef.Id == null) continue;
            var parts = vmRef.Id.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8) continue;
            var vmSubscription = SubscriptionIdentifier.From(parts[1]);
            var vmResourceGroup = ResourceGroupIdentifier.From(parts[3]);
            var vmName = parts[7];
            var vmResult = _virtualMachineControlPlane.Get(vmSubscription, vmResourceGroup, vmName);
            if (vmResult.Result != OperationResult.Success) continue;
            var vmSize = vmResult.Resource?.Properties.HardwareProfile?.GetProperty("vmSize").GetString();
            if (vmSize == null) continue;
            var family = ExtractVmFamily(vmSize);
            if (family != null) families.Add(family);
        }

        if (families.Count == 0)
        {
            return new ControlPlaneOperationResult<VirtualMachineSize[]>(OperationResult.Success, allSizes);
        }

        var filtered = allSizes.Where(s => s.Name != null && families.Contains(ExtractVmFamily(s.Name) ?? string.Empty)).ToArray();
        return new ControlPlaneOperationResult<VirtualMachineSize[]>(OperationResult.Success, filtered);
    }

    private static string? ExtractVmFamily(string sizeName)
    {
        // Format: Standard_{Family}{digits}... e.g. Standard_D2s_v3 -> D, Standard_ND6s -> ND
        const string prefix = "Standard_";
        if (!sizeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var rest = sizeName[prefix.Length..];
        var family = new string(rest.TakeWhile(char.IsLetter).ToArray());
        return family.Length > 0 ? family : null;
    }

    public ControlPlaneOperationResult<AvailabilitySetResource[]> ListBySubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        var subscription = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AvailabilitySetResource[]>(OperationResult.NotFound, null, subscription.Reason, subscription.Code);
        }
        
        var resources = provider.ListAs<AvailabilitySetResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<AvailabilitySetResource[]>(OperationResult.Success, resources);
    }

    public ControlPlaneOperationResult<AvailabilitySetResource> Update(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string availabilitySetName, CreateOrUpdateAvailabilitySetRequest request)
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
        
        var existing = Get(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName);
        if (existing.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AvailabilitySetResource>(OperationResult.NotFound, null,
                $"Availability set '{availabilitySetName}' not found.", "AvailabilitySetNotFound");
        }
        
        existing.Resource!.UpdateFromRequest(request);
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName, existing);

        return new ControlPlaneOperationResult<AvailabilitySetResource>(
            OperationResult.Updated,
            existing.Resource);
    }
}