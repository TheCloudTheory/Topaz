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
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-web-sites");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "app-service-site";
    public string Name => "Azure App Service Site";
    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CheckAppServiceNameAvailabilityEndpoint(eventPipeline, logger),
        new GetWebAppStacksEndpoint(eventPipeline, logger),
        new ListAppServiceSitesByResourceGroupEndpoint(eventPipeline, logger),
        new ListAppServiceSitesBySubscriptionEndpoint(eventPipeline, logger),
        new CreateOrUpdateAppServiceSiteEndpoint(eventPipeline, logger),
        new GetAppServiceSiteEndpoint(eventPipeline, logger),
        new GetSiteConfigWebEndpoint(eventPipeline, logger),
        new UpdateSiteConfigWebEndpoint(eventPipeline, logger),
        new UpdateAppSettingsEndpoint(eventPipeline, logger),
        new ListAppSettingsEndpoint(eventPipeline, logger),
        new GetSlotConfigNamesEndpoint(eventPipeline, logger),
        new DeleteAppServiceSiteEndpoint(eventPipeline, logger),
        new PostPublishXmlEndpoint(logger)
    ];
}
