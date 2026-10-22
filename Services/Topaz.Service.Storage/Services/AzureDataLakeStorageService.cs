using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Adls;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class AzureDataLakeStorageService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".adls");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "adls";

    public string Name => "Azure Data Lake Storage";
    
    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateFileSystemEndpoint(eventPipeline, logger)
    ];
}