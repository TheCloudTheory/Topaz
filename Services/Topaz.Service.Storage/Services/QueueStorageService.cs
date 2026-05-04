using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Queue;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public class QueueStorageService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "queuestorage";
    public string Name => "Queue Storage";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AzureStorageService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new GetQueueServicePropertiesEndpoint(eventPipeline, logger),
        new SetQueueServicePropertiesEndpoint(eventPipeline, logger),
        new GetQueueServiceStatsEndpoint(eventPipeline, logger),
        new SetQueueMetadataEndpoint(eventPipeline, logger),
        new SetQueueAclEndpoint(eventPipeline, logger),
        new CreateQueueEndpoint(eventPipeline, logger),
        new DeleteQueueEndpoint(eventPipeline, logger),
        new ListQueuesEndpoint(eventPipeline, logger),
        new GetQueueAclEndpoint(eventPipeline, logger),
        new GetQueuePropertiesEndpoint(eventPipeline, logger),
        new SendMessageEndpoint(eventPipeline, logger),
        new PeekMessagesEndpoint(eventPipeline, logger),
        new GetMessagesEndpoint(eventPipeline, logger),
        new PutMessageEndpoint(eventPipeline, logger),
        new ClearMessagesEndpoint(eventPipeline, logger),
        new DeleteMessageEndpoint(eventPipeline, logger),
    ];

    public void Bootstrap()
    {
    }
}
