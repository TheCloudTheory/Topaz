using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

public sealed class AppConfigurationService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    private readonly Pipeline _eventPipeline = eventPipeline;
    private readonly ITopazLogger _logger = logger;
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".app-configuration");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "appconfig";

    public string Name => "Azure App Configuration";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [];

    public void Bootstrap() { }
}

