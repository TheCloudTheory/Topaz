using Topaz.EventPipeline;
using Topaz.Service.AppConfiguration.Endpoints;
using Topaz.Service.AppConfiguration.Endpoints.DataPlane;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

public sealed class AppConfigurationService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".app-configuration");
    public static IReadOnlyCollection<string>? Subresources => ["access-keys", "kv"];
    public static string UniqueName => "appconfig";

    public string Name => "Azure App Configuration";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateConfigurationStoreEndpoint(eventPipeline, logger),
        new GetConfigurationStoreEndpoint(eventPipeline, logger),
        new DeleteConfigurationStoreEndpoint(eventPipeline, logger),
        new UpdateConfigurationStoreEndpoint(eventPipeline, logger),
        new ListConfigurationStoresByResourceGroupEndpoint(eventPipeline, logger),
        new ListConfigurationStoresBySubscriptionEndpoint(eventPipeline, logger),
        new ListKeysConfigurationStoreEndpoint(eventPipeline, logger),
        new RegenerateKeyConfigurationStoreEndpoint(eventPipeline, logger),
        new ListConfigurationStoreReplicasEndpoint(eventPipeline, logger),
        new PurgeConfigurationStoreEndpoint(eventPipeline, logger),
        new ListDeletedStoresEndpoint(eventPipeline, logger),
        new GetDeletedStoreEndpoint(eventPipeline, logger),
        new ListKeyValuesEndpoint(eventPipeline, logger),
        new GetKeyValueEndpoint(eventPipeline, logger),
        new SetKeyValueEndpoint(eventPipeline, logger),
        new DeleteKeyValueEndpoint(eventPipeline, logger),
        new ListLabelsEndpoint(eventPipeline, logger),
        new GetRevisionsEndpoint(eventPipeline, logger),
        new LockKeyValueEndpoint(eventPipeline, logger),
        new UnlockKeyValueEndpoint(eventPipeline, logger),
    ];
}

