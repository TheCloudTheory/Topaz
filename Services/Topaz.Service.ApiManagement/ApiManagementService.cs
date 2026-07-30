using JetBrains.Annotations;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

[UsedImplicitly]
public sealed class ApiManagementService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".apim");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "apim";

    public string Name => "API Management";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateApiManagementServiceEndpoint(eventPipeline, logger)
    ];
}