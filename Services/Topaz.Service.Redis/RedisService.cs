using Topaz.EventPipeline;
using Topaz.Service.Redis.Endpoints;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

public sealed class RedisService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".redis");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "redis";

    public string Name => "Redis";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateRedisEndpoint(eventPipeline, logger)
    ];
}