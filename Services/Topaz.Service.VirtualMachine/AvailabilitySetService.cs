using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.VirtualMachine.Endpoints.AvailabilitySets;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine;

public sealed class AvailabilitySetService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".availability-set");
    public static IReadOnlyCollection<string>? Subresources => [];
    public static string UniqueName => "availability-sets";
    public string Name => "Availability Sets";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateAvailabilitySetEndpoint(eventPipeline, logger),
        new GetAvailabilitySetEndpoint(eventPipeline, logger),
        new DeleteAvailabilitySetEndpoint(eventPipeline, logger),
        new ListAvailabilitySetsEndpoint(eventPipeline, logger),
        new ListAvailableSizesEndpoint(eventPipeline, logger),
        new ListAvalabilitySetsBySubscriptionEndpoint(eventPipeline, logger),
        new UpdateAvailabilitySetEndpoint(eventPipeline, logger)
    ];
}