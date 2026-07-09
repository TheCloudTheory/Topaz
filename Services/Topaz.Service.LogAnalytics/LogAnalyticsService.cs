using Topaz.EventPipeline;
using Topaz.Service.LogAnalytics.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics;

public sealed class LogAnalyticsService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".log-analytics");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "loganalytics";

    public string Name => "Azure Log Analytics";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateWorkspaceEndpoint(_eventPipeline, _logger),
        new GetWorkspaceEndpoint(_eventPipeline, _logger),
        new DeleteWorkspaceEndpoint(_eventPipeline, _logger),
        new UpdateWorkspaceEndpoint(_eventPipeline, _logger),
        new ListWorkspacesByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListWorkspacesBySubscriptionEndpoint(_eventPipeline, _logger),
    ];

    public void Bootstrap() { }
}
