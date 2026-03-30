using Topaz.EventPipeline;
using Topaz.Service.ManagedIdentity.Endpoints;
using Topaz.Service.ManagedIdentity.Endpoints.FederatedIdentityCredentials;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

public sealed class ManagedIdentityService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".managed-identity");
    public static IReadOnlyCollection<string>? Subresources => ["federatedIdentityCredentials"];
    public static string UniqueName => "managedidentity";
    public string Name => "Managed Identity";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ManagedIdentityEndpoint(eventPipeline, logger),
        new GetSystemAssignedIdentityByResourceEndpoint(logger),
        new CreateOrUpdateFederatedIdentityCredentialEndpoint(logger),
        new GetFederatedIdentityCredentialEndpoint(logger),
        new DeleteFederatedIdentityCredentialEndpoint(logger),
        new ListFederatedIdentityCredentialsEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}