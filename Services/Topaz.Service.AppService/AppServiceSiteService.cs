using JetBrains.Annotations;
using Topaz.EventPipeline;
using Topaz.Service.AppService.Endpoints.Sites;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService;

[UsedImplicitly]
public sealed class AppServiceSiteService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-web-sites");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "app-service-site";
    public string Name => "Azure App Service Site";
    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CheckAppServiceNameAvailabilityEndpoint(_eventPipeline, _logger),
        new GetWebAppStacksEndpoint(_eventPipeline, _logger),
        new ListAppServiceSitesByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListAppServiceSitesBySubscriptionEndpoint(_eventPipeline, _logger),
        new CreateOrUpdateAppServiceSiteEndpoint(_eventPipeline, _logger),
        new GetAppServiceSiteEndpoint(_eventPipeline, _logger),
        new GetSiteConfigWebEndpoint(_eventPipeline, _logger),
        new DeleteAppServiceSiteEndpoint(_eventPipeline, _logger),
        new PostPublishXmlEndpoint(_logger)
    ];
}
