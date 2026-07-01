using Topaz.EventPipeline;
using Topaz.Service.AppConfiguration.Endpoints;
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
    public static IReadOnlyCollection<string>? Subresources => ["access-keys"];
    public static string UniqueName => "appconfig";

    public string Name => "Azure App Configuration";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateConfigurationStoreEndpoint(_eventPipeline, _logger),
        new GetConfigurationStoreEndpoint(_eventPipeline, _logger),
        new DeleteConfigurationStoreEndpoint(_eventPipeline, _logger),
        new UpdateConfigurationStoreEndpoint(_eventPipeline, _logger),
        new ListConfigurationStoresByResourceGroupEndpoint(_eventPipeline, _logger),
        new ListConfigurationStoresBySubscriptionEndpoint(_eventPipeline, _logger),
        new ListKeysConfigurationStoreEndpoint(_eventPipeline, _logger),
        new RegenerateKeyConfigurationStoreEndpoint(_eventPipeline, _logger)
    ];

    public void Bootstrap() { }
}

