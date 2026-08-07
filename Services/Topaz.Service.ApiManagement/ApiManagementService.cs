using JetBrains.Annotations;
using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Identity;
using Topaz.Service.ApiManagement.Endpoints;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Api;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Backend;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Policy;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.PortalSettings;
using Topaz.Service.ApiManagement.Endpoints.DataPlane.Product;
using Topaz.Service.ApiManagement.Models;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using GetBackendEntityTagEndpoint = Topaz.Service.ApiManagement.Endpoints.DataPlane.Backend.GetBackendEntityTagEndpoint;

namespace Topaz.Service.ApiManagement;

[UsedImplicitly]
public sealed class ApiManagementService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".apim");

    public static IReadOnlyCollection<string>? Subresources =>
    [
        "apis", "apis-etag", "apis-revision", "products", "products-etag", "products-subscriptions",
        "productapiassignment", "backends", "backends-etag", 
        "policies", "policies-etag", "portalsettings", "portalsettings-etag"
    ];
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
        new ListApiAssignmentsByProductEndpoint(eventPipeline, logger),
        new UpdateProductEndpoint(eventPipeline, logger),
        new CreateOrUpdateBackendEndpoint(eventPipeline, logger),
        new GetBackendEndpoint(eventPipeline, logger),
        new DeleteBackendEndpoint(eventPipeline, logger),
        new GetBackendEntityTagEndpoint(eventPipeline, logger),
        new ListBackendByServiceEndpoint(eventPipeline, logger),
        new ReconnectBackendEndpoint(),
        new UpdateBackendEndpoint(eventPipeline, logger),
        new CreateOrUpdatePolicyEndpoint(eventPipeline, logger),
        new DeletePolicyEndpoint(eventPipeline, logger),
        new GetPolicyEndpoint(eventPipeline, logger),
        new ListPolicyByServiceEndpoint(eventPipeline, logger),
        new GetPolicyEntityTagEndpoint(eventPipeline, logger),
        new GetSignInSettingsEndpoint(eventPipeline, logger),
        new CreateOrUpdateSignInSettingsEndpoint(eventPipeline, logger),
        new UpdateSignInSettingsEndpoint(eventPipeline, logger),
        new GetSignInSettingsEntityTagEndpoint(eventPipeline, logger),
        new CreateOrUpdateSignUpSettingsEndpoint(eventPipeline, logger),
        new GetSignUpSettingsEndpoint(eventPipeline, logger),
        new GetSignUpSettingsEntityTagEndpoint(eventPipeline, logger),
        new UpdateSignUpSettingsEndpoint(eventPipeline, logger),
        new CreateOrUpdateDelegationSettingsEndpoint(eventPipeline, logger),
        new GetDelegationSettingsEndpoint(eventPipeline, logger),
        new GetDelegationSettingsEntityTagEndpoint(eventPipeline, logger),
        new ListDelegationSettingsSecretsEndpoint(eventPipeline, logger),
        new UpdateDelegationSettingsEndpoint(eventPipeline, logger)
    ];
}