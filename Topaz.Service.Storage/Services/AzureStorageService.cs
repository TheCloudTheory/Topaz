using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class AzureStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, "{resourceGroup}", ".azure-storage");
    public static IReadOnlyCollection<string>? Subresources => null;

    public string Name => "Azure Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new AzureStorageEndpoint(logger)
    ];
}
