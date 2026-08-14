using Azure;
using Azure.Core;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using Topaz.Portal.Models.Redis;

namespace Topaz.Portal.Clients;

internal sealed partial class TopazClient
{
    public async Task<ListRedisCachesResponse> ListRedisCaches()
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var caches = new List<RedisDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var redis in subscriptionResource.GetAllRedisAsync())
            {
                caches.Add(new RedisDto
                {
                    Id = redis.Id.ToString(),
                    Name = redis.Data.Name,
                    Location = redis.Data.Location,
                    ResourceGroupName = redis.Id.ResourceGroupName,
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionName = subscription.DisplayName,
                    SkuName = redis.Data.Sku?.Name.ToString(),
                    HostName = redis.Data.HostName,
                    Port = redis.Data.Port,
                    SslPort = redis.Data.SslPort,
                    RedisVersion = redis.Data.RedisVersion,
                    ProvisioningState = redis.Data.ProvisioningState?.ToString(),
                    Tags = redis.Data.Tags != null
                        ? new Dictionary<string, string>(redis.Data.Tags, StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                });
            }
        }

        return new ListRedisCachesResponse { Value = caches.ToArray() };
    }

    public async Task CreateRedisCache(
        Guid subscriptionId,
        string resourceGroupName,
        string name,
        string location,
        string skuName = "Basic",
        int capacity = 1,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var family = skuName.Equals("Premium", StringComparison.OrdinalIgnoreCase)
            ? RedisSkuFamily.Premium
            : RedisSkuFamily.BasicOrStandard;

        var skuNameValue = skuName switch
        {
            "Standard" => RedisSkuName.Standard,
            "Premium" => RedisSkuName.Premium,
            _ => RedisSkuName.Basic
        };

        var content = new RedisCreateOrUpdateContent(new AzureLocation(location), new RedisSku(skuNameValue, family, capacity));

        _ = await rg.Value.GetAllRedis().CreateOrUpdateAsync(
            WaitUntil.Completed,
            name,
            content,
            cancellationToken);
    }

    public async Task<RedisDto?> GetRedisCache(
        Guid subscriptionId,
        string resourceGroupName,
        string name,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var redisId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Cache/Redis/{name}");

        var redis = await _armClient!.GetRedisResource(redisId).GetAsync(cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return new RedisDto
        {
            Id = redis.Value.Id.ToString(),
            Name = redis.Value.Data.Name,
            Location = redis.Value.Data.Location,
            ResourceGroupName = resourceGroupName,
            SubscriptionId = subscriptionId.ToString(),
            SubscriptionName = subscription.Value.Data.DisplayName,
            SkuName = redis.Value.Data.Sku?.Name.ToString(),
            HostName = redis.Value.Data.HostName,
            Port = redis.Value.Data.Port,
            SslPort = redis.Value.Data.SslPort,
            RedisVersion = redis.Value.Data.RedisVersion,
            ProvisioningState = redis.Value.Data.ProvisioningState?.ToString(),
            Tags = redis.Value.Data.Tags != null
                ? new Dictionary<string, string>(redis.Value.Data.Tags, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task CreateOrUpdateRedisCacheTag(
        Guid subscriptionId,
        string resourceGroupName,
        string name,
        string tagKey,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var redisId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Cache/Redis/{name}");

        var redis = await _armClient!.GetRedisResource(redisId).GetAsync(cancellationToken);
        redis.Value.Data.Tags[tagKey] = tagValue;

        await redis.Value.UpdateAsync(WaitUntil.Completed, 
            new RedisPatch { Tags = { [tagKey] = tagValue } },
            cancellationToken);
    }

    public async Task DeleteRedisCacheTag(
        Guid subscriptionId,
        string resourceGroupName,
        string name,
        string tagKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var redisId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Cache/Redis/{name}");

        var redis = await _armClient!.GetRedisResource(redisId).GetAsync(cancellationToken);
        
        await redis.Value.RemoveTagAsync(tagKey, cancellationToken);
    }
}
