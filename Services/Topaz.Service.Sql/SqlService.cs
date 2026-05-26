using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Sql;

public sealed class SqlService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-sql");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "sql";

    public string Name => "Azure SQL";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];

    public void Bootstrap() { }
}
