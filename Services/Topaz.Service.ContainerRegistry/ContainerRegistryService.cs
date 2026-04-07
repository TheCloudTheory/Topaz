using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

public sealed class ContainerRegistryService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".container-registry");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "container-registry";

    public string Name => "Azure Container Registry";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateContainerRegistryEndpoint(eventPipeline, logger),
        new GetContainerRegistryEndpoint(eventPipeline, logger),
        new ListContainerRegistriesByResourceGroupEndpoint(eventPipeline, logger),
        new ListContainerRegistriesBySubscriptionEndpoint(eventPipeline, logger),
        new DeleteContainerRegistryEndpoint(eventPipeline, logger),
        new UpdateContainerRegistryEndpoint(eventPipeline, logger),
        new CheckContainerRegistryNameAvailabilityEndpoint(eventPipeline, logger),
        new AcrV2ChallengeEndpoint(ContainerRegistryControlPlane.New(eventPipeline, logger), logger),
        new AcrOAuth2ExchangeEndpoint(logger),
        new AcrOAuth2GetTokenEndpoint(ContainerRegistryControlPlane.New(eventPipeline, logger), logger),
        new AcrOAuth2PostTokenEndpoint(ContainerRegistryControlPlane.New(eventPipeline, logger), logger),
        new ListContainerRegistryCredentialsEndpoint(eventPipeline, logger),
        new GenerateContainerRegistryCredentialsEndpoint(eventPipeline, logger),
        new RegenerateContainerRegistryCredentialEndpoint(eventPipeline, logger),
        new ListContainerRegistryUsagesEndpoint(eventPipeline, logger),
        // Data plane — blob uploads (OCI Distribution Spec)
        new InitiateBlobUploadEndpoint(AcrDataPlane(), logger),
        new PatchBlobUploadEndpoint(AcrDataPlane(), logger),
        new CompleteBlobUploadEndpoint(AcrDataPlane(), logger),
        new HeadBlobEndpoint(AcrDataPlane(), logger),
        new GetBlobEndpoint(AcrDataPlane(), logger),
        new PutManifestEndpoint(AcrDataPlane(), logger),
        new GetManifestEndpoint(AcrDataPlane(), logger),
        new ListRepositoriesEndpoint(AcrDataPlane(), logger),
        new ListTagsEndpoint(AcrDataPlane(), logger),
    ];

    public void Bootstrap()
    {
    }

    private AcrDataPlane AcrDataPlane() =>
        new(new ContainerRegistryResourceProvider(logger), logger);
}
