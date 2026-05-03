using Topaz.EventPipeline;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Service.VirtualMachine.Models;
using Topaz.Service.VirtualMachine.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

internal sealed class VirtualMachineServiceControlPlane(
    Pipeline eventPipeline,
    VirtualMachineResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string VirtualMachineNotFoundCode = "VMNotFound";
    private const string VirtualMachineNotFoundMessageTemplate = "Virtual machine '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static VirtualMachineServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new VirtualMachineResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var vm = resource.As<VirtualMachineResource, VirtualMachineResourceProperties>();
        if (vm == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Virtual Machine instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(vm.Location))
        {
            logger.LogError($"Virtual machine resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                vm.GetSubscription(),
                vm.GetResourceGroup(),
                vm.Name,
                new CreateOrUpdateVirtualMachineRequest
                {
                    Location = vm.Location,
                    Tags = vm.Tags,
                    Properties = new CreateOrUpdateVirtualMachineRequest.CreateOrUpdateVirtualMachineRequestProperties
                    {
                        HardwareProfile = vm.Properties.HardwareProfile,
                        StorageProfile = vm.Properties.StorageProfile,
                        OsProfile = vm.Properties.OsProfile,
                        NetworkProfile = vm.Properties.NetworkProfile
                    }
                });

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<VirtualMachineResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualMachineName)
    {
        var resource = provider.GetAs<VirtualMachineResource>(subscriptionIdentifier, resourceGroupIdentifier,
            virtualMachineName);

        return resource == null
            ? new ControlPlaneOperationResult<VirtualMachineResource>(
                OperationResult.NotFound,
                null,
                string.Format(VirtualMachineNotFoundMessageTemplate, virtualMachineName),
                VirtualMachineNotFoundCode)
            : new ControlPlaneOperationResult<VirtualMachineResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<VirtualMachineResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualMachineName,
        CreateOrUpdateVirtualMachineRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<VirtualMachineResource>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<VirtualMachineResource>(subscriptionIdentifier, resourceGroupIdentifier,
            virtualMachineName);

        if (existing != null)
        {
            existing.Location = request.Location?.ToString() ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            VirtualMachineResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, virtualMachineName, existing);

            return new ControlPlaneOperationResult<VirtualMachineResource>(OperationResult.Updated, existing, null,
                null);
        }

        var location = request.Location ?? resourceGroupOperation.Resource!.Location!;
        var properties = VirtualMachineResourceProperties.FromRequest(request);
        var resource = new VirtualMachineResource(subscriptionIdentifier, resourceGroupIdentifier, virtualMachineName,
            location, request.Tags, properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, virtualMachineName, resource,
            createOperation: true);

        return new ControlPlaneOperationResult<VirtualMachineResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string virtualMachineName)
    {
        var resource = provider.GetAs<VirtualMachineResource>(subscriptionIdentifier, resourceGroupIdentifier,
            virtualMachineName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(VirtualMachineNotFoundMessageTemplate, virtualMachineName),
                VirtualMachineNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, virtualMachineName);

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<VirtualMachineResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<VirtualMachineResource>(subscriptionIdentifier, resourceGroupIdentifier)
            .ToArray();

        return new ControlPlaneOperationResult<VirtualMachineResource[]>(OperationResult.Success, resources, null,
            null);
    }

    public ControlPlaneOperationResult<VirtualMachineResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<VirtualMachineResource>(subscriptionIdentifier, null).ToArray();

        return new ControlPlaneOperationResult<VirtualMachineResource[]>(OperationResult.Success, resources, null,
            null);
    }
}
