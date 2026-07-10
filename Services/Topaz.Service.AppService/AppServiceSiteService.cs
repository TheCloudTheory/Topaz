using JetBrains.Annotations;
using Topaz.Service.AppService.Endpoints.Sites;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService;

[UsedImplicitly]
public sealed class AppServiceSiteService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-web-sites");
    public static IReadOnlyCollection<string>? Subresources => ["publishingcredentials"];
    public static string UniqueName => "app-service-site";
    public string Name => "Azure App Service Site";
    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CheckAppServiceNameAvailabilityEndpoint(logger),
        new GetWebAppStacksEndpoint(logger),
        new ListAppServiceSitesByResourceGroupEndpoint(logger),
        new ListAppServiceSitesBySubscriptionEndpoint(logger),
        new CreateOrUpdateAppServiceSiteEndpoint(logger),
        new GetAppServiceSiteEndpoint(logger),
        new GetSiteConfigWebEndpoint(logger),
        new UpdateSiteConfigWebEndpoint(logger),
        new UpdateAppSettingsEndpoint(logger),
        new ListAppSettingsEndpoint(logger),
        new GetSlotConfigNamesEndpoint(logger),
        new DeleteAppServiceSiteEndpoint(logger),
        new PostPublishXmlEndpoint(logger)
    ];
}
