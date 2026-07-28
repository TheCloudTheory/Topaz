using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure Redis Cache resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateRedisCacheTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Redis Cache in the given resource group and returns its hostname, SSL port, and primary access key.")]
    [UsedImplicitly]
    public static async Task<RedisCacheResult> CreateRedisCache(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the Redis cache to create.")]
        string cacheName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var sku = new RedisSku(RedisSkuName.Basic, RedisSkuFamily.BasicOrStandard, 0);
        var content = new RedisCreateOrUpdateContent(new AzureLocation(location), sku);
        var operation = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, content)
            .ConfigureAwait(false);

        var cache = operation.Value;
        var keys = await cache.GetKeysAsync().ConfigureAwait(false);
        var primaryKey = keys.Value.PrimaryKey ?? string.Empty;
        var hostName = cache.Data.HostName ?? $"{cacheName}.redis.cache.topaz.local.dev";
        var sslPort = cache.Data.SslPort ?? 6380;

        return new RedisCacheResult
        {
            CacheName = cacheName,
            HostName = hostName,
            SslPort = sslPort,
            PrimaryKey = primaryKey,
            ConnectionString = TopazResourceHelpers.GetRedisConnectionString(cacheName, primaryKey, sslPort),
        };
    }

    public sealed record RedisCacheResult
    {
        public required string CacheName { [UsedImplicitly] get; init; }
        public required string HostName { [UsedImplicitly] get; init; }
        public required int SslPort { [UsedImplicitly] get; init; }
        public required string PrimaryKey { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
    }
}
