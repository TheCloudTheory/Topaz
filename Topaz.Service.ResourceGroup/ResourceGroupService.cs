using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup.Endpoints;
using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public sealed class ResourceGroupService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".resource-group", "{resourceGroup}");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "resourcegroup";

    public string Name => "Resource Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateUpdateResourceGroupEndpoint(eventPipeline, logger),
        new GetResourceGroupEndpoint(eventPipeline, logger),
        new ListResourceGroupEndpoint(eventPipeline, logger),
        new DeleteResourceGroupEndpoint(eventPipeline, logger),
    ];

    public void Bootstrap()
    {
    }
}
