using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.Redis.Models;
using Topaz.Service.Redis.Models.Requests;
using Topaz.Service.Redis.Models.Responses;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Redis;

internal sealed class RedisServiceControlPlane(
    Pipeline eventPipeline,
    RedisResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "Redis resource '{0}' could not be found.";
    
    private const string AccessKeysSubresource = "access-keys";
    private const string AccessKeysId = "keys";
    private const string FirewallRuleSubresource = "firewall-rules";
    
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static RedisServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new RedisResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var store = resource.As<RedisResource, RedisResourceProperties>();
        if (store == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Redis instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(store.Location))
        {
            logger.LogError($"Redis resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(store.GetSubscription(), store.GetResourceGroup(), store.Name, store);
            return result.Result is OperationResult.Created or OperationResult.Updated
                ? OperationResult.Success
                : OperationResult.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }
    
    public ControlPlaneOperationResult<RedisResource> CreateOrUpdate(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        RedisResource request)
    {
        var rgOp = _resourceGroupControlPlane.Get(sub, rg);
        if (rgOp.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<RedisResource>(
                OperationResult.NotFound, null, rgOp.Reason, rgOp.Code);

        var existing = provider.GetAs<RedisResource>(sub, rg, name);

        if (existing != null)
        {
            existing.Tags = request.Tags ?? existing.Tags;
            existing.Properties.UpdateFromRequest(request.Properties);

            if (request.Sku?.Name != null)
            {
                var updated = new RedisResource(
                    sub, rg, name, existing.Location!, existing.Tags,
                    new ResourceSku { Name = request.Sku.Name }, existing.Properties);
                provider.CreateOrUpdate(sub, rg, name, updated);
                return new ControlPlaneOperationResult<RedisResource>(OperationResult.Updated, updated);
            }

            provider.CreateOrUpdate(sub, rg, name, existing);
            return new ControlPlaneOperationResult<RedisResource>(OperationResult.Updated, existing);
        }

        var location = request.Location ?? rgOp.Resource!.Location!;
        var resource = new RedisResource(sub, rg, name, location, request.Tags, request.Sku, request.Properties);

        provider.CreateOrUpdate(sub, rg, name, resource, createOperation: true);
        
        var keyStore = RedisAccessKeyStore.Generate(name);
        provider.CreateOrUpdateSubresource(sub, rg, AccessKeysId, name, AccessKeysSubresource, keyStore);

        return new ControlPlaneOperationResult<RedisResource>(OperationResult.Created, resource);
    }

    public ControlPlaneOperationResult<RedisResource> Get(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<RedisResource>(sub, rg, name);
        return resource == null
            ? new ControlPlaneOperationResult<RedisResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<RedisResource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<RedisResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, string.Format(NotFoundMessage, name), NotFoundCode);

        provider.Delete(sub, rg, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<RedisResource[]> ListByResourceGroup(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg)
    {
        var resources = provider.ListAs<RedisResource>(sub, rg, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub) && r.IsInResourceGroup(rg))
            .ToArray();
        return new ControlPlaneOperationResult<RedisResource[]>(OperationResult.Success, resources);
    }

    public ControlPlaneOperationResult<RedisResource[]> ListBySubscription(SubscriptionIdentifier sub)
    {
        var resources = provider.ListAs<RedisResource>(sub, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub))
            .ToArray();
        return new ControlPlaneOperationResult<RedisResource[]>(OperationResult.Success, resources);
    }

    public ControlPlaneOperationResult<RedisAccessKeysResponse> ListKeys(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<RedisResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult<RedisAccessKeysResponse>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        var keyStore = provider.GetSubresourceAs<RedisAccessKeyStore>(
            sub, rg, AccessKeysId, name, AccessKeysSubresource);
        var primary = keyStore?.Keys.FirstOrDefault(k => k.Name == "Primary")?.Value ?? string.Empty;
        var secondary = keyStore?.Keys.FirstOrDefault(k => k.Name == "Secondary")?.Value ?? string.Empty;
        return new ControlPlaneOperationResult<RedisAccessKeysResponse>(
            OperationResult.Success, new RedisAccessKeysResponse(primary, secondary));
    }

    public ControlPlaneOperationResult<RedisAccessKeysResponse> RegenerateKey(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        string keyType)
    {
        var resource = provider.GetAs<RedisResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult<RedisAccessKeysResponse>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        var keyStore = provider.GetSubresourceAs<RedisAccessKeyStore>(
            sub, rg, AccessKeysId, name, AccessKeysSubresource);
        if (keyStore == null)
            return new ControlPlaneOperationResult<RedisAccessKeysResponse>(
                OperationResult.NotFound, null, $"Access keys not found for cache '{name}'.", NotFoundCode);

        var key = keyStore.Keys.FirstOrDefault(
            k => string.Equals(k.Name, keyType, StringComparison.OrdinalIgnoreCase));
        if (key == null)
            return new ControlPlaneOperationResult<RedisAccessKeysResponse>(
                OperationResult.NotFound, null, $"Key type '{keyType}' not found.", NotFoundCode);

        key.Value = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(44));
        provider.CreateOrUpdateSubresource(sub, rg, AccessKeysId, name, AccessKeysSubresource, keyStore);

        var primary = keyStore.Keys.FirstOrDefault(k => k.Name == "Primary")?.Value ?? string.Empty;
        var secondary = keyStore.Keys.FirstOrDefault(k => k.Name == "Secondary")?.Value ?? string.Empty;
        return new ControlPlaneOperationResult<RedisAccessKeysResponse>(
            OperationResult.Success, new RedisAccessKeysResponse(primary, secondary));
    }

    public ControlPlaneOperationResult<FirewallRule> CreateOrUpdateFirewallRule(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string cacheName,
        string ruleName,
        CreateOrUpdateFirewallRuleRequest request)
    {
        var existingCache = Get(subscriptionIdentifier, resourceGroupIdentifier, cacheName);
        if (existingCache.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<FirewallRule>(OperationResult.NotFound, null, existingCache.Reason,
                existingCache.Code);
        }

        if (existingCache.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<FirewallRule>(OperationResult.Failed, null, existingCache.Reason,
                existingCache.Code);
        }

        var existingRule = GetFirewallRule(subscriptionIdentifier, resourceGroupIdentifier, cacheName, ruleName);
        (bool IsValid, string? Error) validationResult;
        if (existingRule.Result == OperationResult.NotFound)
        {
            var rule = FirewallRule.FromRequest(subscriptionIdentifier, resourceGroupIdentifier, cacheName, ruleName,
                request);

            validationResult = rule.Validate<FirewallRule>();
            if (!validationResult.IsValid)
            {
                return new ControlPlaneOperationResult<FirewallRule>(OperationResult.BadRequest, null,
                    validationResult.Error);
            }

            provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, ruleName, cacheName,
                FirewallRuleSubresource,
                rule);

            return new ControlPlaneOperationResult<FirewallRule>(OperationResult.Created, rule);
        }

        existingRule.Resource!.UpdateFromRequest(request);
        validationResult = existingRule.Resource.Validate<FirewallRule>();
        
        return !validationResult.IsValid
            ? new ControlPlaneOperationResult<FirewallRule>(OperationResult.BadRequest, null, validationResult.Error)
            : new ControlPlaneOperationResult<FirewallRule>(OperationResult.Updated, existingRule.Resource!);
    }

    internal ControlPlaneOperationResult<FirewallRule> GetFirewallRule(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string cacheName, string ruleName)
    {
        var existingCache = Get(subscriptionIdentifier, resourceGroupIdentifier, cacheName);
        if (existingCache.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<FirewallRule>(OperationResult.NotFound, null, existingCache.Reason,
                existingCache.Code);
        }

        if (existingCache.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<FirewallRule>(OperationResult.Failed, null, existingCache.Reason,
                existingCache.Code);
        }

        var rule = provider.GetSubresourceAs<FirewallRule>(subscriptionIdentifier, resourceGroupIdentifier, ruleName,
            cacheName, FirewallRuleSubresource);
        if (rule == null)
        {
            return new ControlPlaneOperationResult<FirewallRule>(OperationResult.NotFound, null, "Firewall rule not found",
                "NotFound");
        }

        return new ControlPlaneOperationResult<FirewallRule>(OperationResult.Success, rule);
    }

    public ControlPlaneOperationResult<FirewallRule[]> ListFirewallRules(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string cacheName)
    {
        var existingCache = Get(subscriptionIdentifier, resourceGroupIdentifier, cacheName);
        if (existingCache.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<FirewallRule[]>(OperationResult.NotFound, null, existingCache.Reason,
                existingCache.Code);
        }

        if (existingCache.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult<FirewallRule[]>(OperationResult.Failed, null, existingCache.Reason,
                existingCache.Code);
        }

        var rules = provider.ListSubresourcesAs<FirewallRule>(subscriptionIdentifier, resourceGroupIdentifier,
            cacheName, FirewallRuleSubresource);
        
        return new ControlPlaneOperationResult<FirewallRule[]>(OperationResult.Success, rules);
    }

    public ControlPlaneOperationResult DeleteRule(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string cacheName, string ruleName)
    {
        var existingCache = Get(subscriptionIdentifier, resourceGroupIdentifier, cacheName);
        if (existingCache.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound,  existingCache.Reason,
                existingCache.Code);
        }

        if (existingCache.Result != OperationResult.Success)
        {
            return new ControlPlaneOperationResult(OperationResult.Failed,  existingCache.Reason,
                existingCache.Code);
        }

        var rule = provider.GetSubresourceAs<FirewallRule>(subscriptionIdentifier, resourceGroupIdentifier, ruleName,
            cacheName, FirewallRuleSubresource);
        if (rule == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, "Firewall rule not found",
                "NotFound");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, ruleName, cacheName, FirewallRuleSubresource);
        
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }
}