using JetBrains.Annotations;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement.Endpoints;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Api;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Product;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

[UsedImplicitly]
public sealed class ApiManagementService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".apim");
    public static IReadOnlyCollection<string>? Subresources => ["apis", "apis-etag", "apis-revision", "products", "products-etag", "products-subscriptions"];
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
        new GetDeletedApiManagementServiceByNameEndpoint(eventPipeline, logger),
        new CreateOrUpdateApiEndpoint(eventPipeline, logger),
        new GetApiEndpoint(eventPipeline, logger),
        new ListApiByServiceEndpoint(eventPipeline, logger),
        new GetApiEntityTagEndpoint(eventPipeline, logger),
        new UpdateApiEndpoint(eventPipeline, logger),
        new DeleteApiEndpoint(eventPipeline, logger),
        new ListApiRevisionsByService(eventPipeline, logger),
        new CreateOrUpdateProductEndpoint(eventPipeline, logger),
        new GetProductEndpoint(eventPipeline, logger),
        new DeleteProductEndpoint(eventPipeline, logger),
        new GetProductEntityTagEndpoint(eventPipeline, logger),
        new ListProductByServiceEndpoint(eventPipeline, logger),
        new CreateOrUpdateProductApiEndpoint(eventPipeline, logger),
        new CheckProductApiAssignmentExistEndpoint(eventPipeline, logger),
        new DeleteProductApiEndpoint(eventPipeline, logger),
        new ListApiAssignmentsByProductEndpoint(eventPipeline, logger)
    ];
}