using Topaz.EventPipeline;
using Topaz.Service.Insights.Endpoints;
using Topaz.Service.Insights.Endpoints.DataPlane;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights;

public sealed class InsightsService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".insights");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "insights";
    public string Name => "Insights";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new InsightsServiceEndpoint(),
        new CreateOrUpdateComponentEndpoint(eventPipeline, logger),
        new GetComponentEndpoint(eventPipeline, logger),
        new GetCurrentBillingFeaturesEndpoint(),
        new PutCurrentBillingFeaturesEndpoint(),
        new DeleteComponentEndpoint(eventPipeline, logger),
        new UpdateComponentEndpoint(eventPipeline, logger),
        new ListComponentsByResourceGroupEndpoint(eventPipeline, logger),
        new ListComponentsBySubscriptionEndpoint(eventPipeline, logger),
        new IngestionEndpoint(eventPipeline, logger)
    ];
}