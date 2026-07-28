using Topaz.EventPipeline;
using Topaz.Service.Redis.Endpoints;
using Topaz.Service.Redis.Endpoints.FirewallRules;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

public sealed class RedisService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".redis");
    public static IReadOnlyCollection<string>? Subresources => ["access-keys", "firewall-rules"];
    public static string UniqueName => "redis";

    public string Name => "Redis";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateRedisEndpoint(eventPipeline, logger),
        new GetRedisEndpoint(eventPipeline, logger),
        new DeleteRedisEndpoint(eventPipeline, logger),
        new UpdateRedisEndpoint(eventPipeline, logger),
        new ListRedisByResourceGroupEndpoint(eventPipeline, logger),
        new ListRedisBySubscriptionEndpoint(eventPipeline, logger),
        new ListRedisKeysEndpoint(eventPipeline, logger),
        new RegenerateRedisKeyEndpoint(eventPipeline, logger),
        new CreateOrUpdateFirewallRuleEndpoint(eventPipeline, logger),
        new GetFirewallRuleEndpoint(eventPipeline, logger),
        new DeleteFirewallRuleEndpoint(eventPipeline, logger),
        new ListFirewallRulesEndpoint(eventPipeline, logger)
    ];
}