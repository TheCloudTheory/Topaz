using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Importer;

internal sealed class ImporterService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => string.Empty;
    public static IReadOnlyCollection<string>? Subresources => [];
    public static string UniqueName => "importer";
    public bool IsTopazService => true;
    public string Name => "Importer";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new SeedResourceEndpoint(eventPipeline, logger)
    ];
}