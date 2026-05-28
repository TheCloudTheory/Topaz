using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualMachine.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

public sealed class VirtualMachineService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".virtual-machine");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "virtual-machine";

    public string Name => "Azure Virtual Machines";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateVirtualMachineEndpoint(eventPipeline, logger),
        new UpdateVirtualMachineEndpoint(eventPipeline, logger),
        new GetVirtualMachineEndpoint(eventPipeline, logger),
        new DeleteVirtualMachineEndpoint(eventPipeline, logger),
        new ListVirtualMachinesByResourceGroupEndpoint(eventPipeline, logger),
        new ListVirtualMachinesBySubscriptionEndpoint(eventPipeline, logger),
        new ListVirtualMachineImageVersionsEndpoint(logger),
        new GetVirtualMachineImageVersionEndpoint(logger)
    ];

    public void Bootstrap() { }
}
