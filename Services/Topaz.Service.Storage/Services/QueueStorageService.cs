using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.Queue;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public class QueueStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static string UniqueName => "queuestorage";
    public string Name => "Queue Storage";
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => AzureStorageService.LocalDirectoryPath;
    public static IReadOnlyCollection<string>? Subresources => null;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new SetQueueMetadataEndpoint(logger),
        new SetQueueAclEndpoint(logger),
        new CreateQueueEndpoint(logger),
        new DeleteQueueEndpoint(logger),
        new ListQueuesEndpoint(logger),
        new GetQueueAclEndpoint(logger),
        new GetQueuePropertiesEndpoint(logger),
        new SendMessageEndpoint(logger),
        new PeekMessagesEndpoint(logger),
        new GetMessagesEndpoint(logger),
        new PutMessageEndpoint(logger),
        new ClearMessagesEndpoint(logger),
        new DeleteMessageEndpoint(logger),
    ];

    public void Bootstrap()
    {
    }
}
