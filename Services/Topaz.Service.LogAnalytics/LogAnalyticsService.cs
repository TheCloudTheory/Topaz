using Topaz.EventPipeline;
using Topaz.Service.LogAnalytics.Endpoints;
using Topaz.Service.LogAnalytics.Endpoints.DataPlane;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics;

public sealed class LogAnalyticsService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".log-analytics");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "loganalytics";

    public string Name => "Azure Log Analytics";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateWorkspaceEndpoint(eventPipeline, logger),
        new GetWorkspaceEndpoint(eventPipeline, logger),
        new DeleteWorkspaceEndpoint(eventPipeline, logger),
        new UpdateWorkspaceEndpoint(eventPipeline, logger),
        new ListWorkspacesByResourceGroupEndpoint(eventPipeline, logger),
        new ListWorkspacesBySubscriptionEndpoint(eventPipeline, logger),
        new ListDeletedWorkspacesEndpoint(),
        new DataCollectionEndpoint(eventPipeline, logger),
        new QueryWorkspaceEndpoint(eventPipeline, logger)
    ];
}
