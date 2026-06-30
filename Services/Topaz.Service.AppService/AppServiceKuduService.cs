using JetBrains.Annotations;
using Topaz.Service.AppService.Endpoints.Kudu;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService;

[UsedImplicitly]
public sealed class AppServiceKuduService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AppServiceSiteService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "app-service-kudu";
    public string Name => "Azure App Service Kudu";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new PostZipDeployEndpoint(logger),
        new GetDeploymentsEndpoint(logger),
        new GetDeploymentByIdEndpoint(logger)
    ];
}
