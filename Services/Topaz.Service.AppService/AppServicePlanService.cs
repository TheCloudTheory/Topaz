using Topaz.EventPipeline;
using Topaz.Service.AppService.Endpoints.Plans;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService;

public sealed class AppServicePlanService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-web-plans");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "app-service-plan";
    public string Name => "Azure App Service Plan";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateAppServicePlanEndpoint(_logger),
        new GetAppServicePlanEndpoint(_logger),
        new DeleteAppServicePlanEndpoint(_logger),
        new ListAppServicePlansByResourceGroupEndpoint(_logger),
        new ListAppServicePlansBySubscriptionEndpoint(_logger),
        new RestartAppServicePlanSitesEndpoint(_logger),
    ];
}
