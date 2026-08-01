using JetBrains.Annotations;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement.Endpoints;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Api;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

[UsedImplicitly]
public sealed class ApiManagementService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".apim");
    public static IReadOnlyCollection<string>? Subresources => ["apis", "apis-etag"];
    public static string UniqueName => "apim";

    public string Name => "API Management";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateApiManagementServiceEndpoint(eventPipeline, logger),
        new DeleteApiManagementServiceEndpoint(eventPipeline, logger),
        new CheckApiManagementServiceNameAvailabilityEndpoint(eventPipeline, logger),
        new GetApiManagementServiceEndpoint(eventPipeline, logger),
        new ListApiManagementServicesByResourceGroupEndpoint(eventPipeline, logger),
        new ListApiManagementServicesEndpoint(eventPipeline, logger),
        new UpdateApiManagementServiceEndpoint(eventPipeline, logger),
        new CreateOrUpdateApiEndpoint(eventPipeline, logger),
        new GetApiEndpoint(eventPipeline, logger),
        new GetDeletedApiManagementServiceByNameEndpoint(eventPipeline, logger)
    ];
}