using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.AppConfiguration.Models;
using Topaz.Service.AppConfiguration.Models.DataPlane;
using Topaz.Service.AppConfiguration.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

internal sealed class AppConfigurationServiceControlPlane(
    Pipeline eventPipeline,
    AppConfigurationResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "Configuration store '{0}' could not be found";
    private const string AccessKeysSubresource = "access-keys";
    private const string AccessKeysId = "keys";
    private const string KvSubresource = "kv";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static AppConfigurationServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new AppConfigurationResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var store = resource.As<ConfigurationStoreResource, ConfigurationStoreResourceProperties>();
        if (store == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a ConfigurationStore instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(store.Location))
        {
            logger.LogError($"ConfigurationStore resource `{resource.Id}` is missing required location.");
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

    public ControlPlaneOperationResult<ConfigurationStoreResource> CreateOrUpdate(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        ConfigurationStoreResource request)
    {
        var rgOp = _resourceGroupControlPlane.Get(sub, rg);
        if (rgOp.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<ConfigurationStoreResource>(
                OperationResult.NotFound, null, rgOp.Reason, rgOp.Code);

        var existing = provider.GetAs<ConfigurationStoreResource>(sub, rg, name);

        if (existing != null)
        {
            existing.Location = request.Location ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            if (request.Properties?.PublicNetworkAccess != null)
                existing.Properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;

            if (request.Sku?.Name != null)
            {
                var updated = new ConfigurationStoreResource(
                    sub, rg, name, existing.Location!, existing.Tags,
                    new ResourceSku { Name = request.Sku.Name }, existing.Properties);
                provider.CreateOrUpdate(sub, rg, name, updated);
                return new ControlPlaneOperationResult<ConfigurationStoreResource>(OperationResult.Updated, updated, null, null);
            }

            provider.CreateOrUpdate(sub, rg, name, existing);
            return new ControlPlaneOperationResult<ConfigurationStoreResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? rgOp.Resource!.Location!;
        var properties = ConfigurationStoreResourceProperties.FromRequest(request.Properties, name);
        var resource = new ConfigurationStoreResource(sub, rg, name, location, request.Tags, request.Sku, properties);

        provider.CreateOrUpdate(sub, rg, name, resource, createOperation: true);

        var keyStore = AppConfigurationAccessKeyStore.Generate(name);
        provider.CreateOrUpdateSubresource(sub, rg, AccessKeysId, name, AccessKeysSubresource, keyStore);

        return new ControlPlaneOperationResult<ConfigurationStoreResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreResource> Get(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ConfigurationStoreResource>(sub, rg, name);
        return resource == null
            ? new ControlPlaneOperationResult<ConfigurationStoreResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<ConfigurationStoreResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ConfigurationStoreResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, string.Format(NotFoundMessage, name), NotFoundCode);

        provider.Delete(sub, rg, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ConfigurationStoreResource> Update(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        UpdateConfigurationStoreRequest request)
    {
        var existing = provider.GetAs<ConfigurationStoreResource>(sub, rg, name);
        if (existing == null)
            return new ControlPlaneOperationResult<ConfigurationStoreResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        if (request.Tags != null)
            existing.Tags = request.Tags;
        if (request.Properties?.PublicNetworkAccess != null)
            existing.Properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;

        if (request.Sku?.Name != null)
        {
            var updated = new ConfigurationStoreResource(
                sub, rg, name, existing.Location!, existing.Tags,
                new ResourceSku { Name = request.Sku.Name }, existing.Properties);
            provider.CreateOrUpdate(sub, rg, name, updated);
            return new ControlPlaneOperationResult<ConfigurationStoreResource>(OperationResult.Updated, updated, null, null);
        }

        provider.CreateOrUpdate(sub, rg, name, existing);
        return new ControlPlaneOperationResult<ConfigurationStoreResource>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreResource[]> ListByResourceGroup(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg)
    {
        var resources = provider.ListAs<ConfigurationStoreResource>(sub, rg, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub) && r.IsInResourceGroup(rg))
            .ToArray();
        return new ControlPlaneOperationResult<ConfigurationStoreResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreResource[]> ListBySubscription(
        SubscriptionIdentifier sub)
    {
        var resources = provider.ListAs<ConfigurationStoreResource>(sub, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub))
            .ToArray();
        return new ControlPlaneOperationResult<ConfigurationStoreResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<List<ConfigurationStoreAccessKey>> ListKeys(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ConfigurationStoreResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult<List<ConfigurationStoreAccessKey>>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        var keyStore = provider.GetSubresourceAs<AppConfigurationAccessKeyStore>(
            sub, rg, AccessKeysId, name, AccessKeysSubresource);
        return new ControlPlaneOperationResult<List<ConfigurationStoreAccessKey>>(
            OperationResult.Success, keyStore?.Keys ?? [], null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreAccessKey> RegenerateKey(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        string keyId)
    {
        var resource = provider.GetAs<ConfigurationStoreResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult<ConfigurationStoreAccessKey>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        var keyStore = provider.GetSubresourceAs<AppConfigurationAccessKeyStore>(
            sub, rg, AccessKeysId, name, AccessKeysSubresource);
        if (keyStore == null)
            return new ControlPlaneOperationResult<ConfigurationStoreAccessKey>(
                OperationResult.NotFound, null, $"Access keys not found for store '{name}'.", NotFoundCode);

        var key = keyStore.Keys.FirstOrDefault(
            k => string.Equals(k.Id, keyId, StringComparison.OrdinalIgnoreCase));
        if (key == null)
            return new ControlPlaneOperationResult<ConfigurationStoreAccessKey>(
                OperationResult.NotFound, null, $"Key '{keyId}' not found.", NotFoundCode);

        key.Regenerate(name);

        provider.CreateOrUpdateSubresource(sub, rg, AccessKeysId, name, AccessKeysSubresource, keyStore);

        return new ControlPlaneOperationResult<ConfigurationStoreAccessKey>(OperationResult.Success, key, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreResource> FindByName(string storeName)
    {
        var identifiers = Dns.GlobalDnsEntries.GetEntry(AppConfigurationService.UniqueName, storeName);
        if (identifiers == null)
            return new ControlPlaneOperationResult<ConfigurationStoreResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, storeName), NotFoundCode);

        return Get(SubscriptionIdentifier.From(identifiers.Value.subscription),
            ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!), storeName);
    }

    public AppConfigurationAccessKeyStore? GetAccessKeys(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string storeName) =>
        provider.GetSubresourceAs<AppConfigurationAccessKeyStore>(sub, rg, AccessKeysId, storeName, AccessKeysSubresource);

    public AppConfigurationKeyValue? GetKv(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string storeName, string key, string? label)
    {
        var id = AppConfigurationKeyValue.ToFileId(key, label);
        return provider.GetSubresourceAs<AppConfigurationKeyValue>(sub, rg, id, storeName, KvSubresource);
    }

    public AppConfigurationKeyValue[] ListKvs(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string storeName, string? keyFilter, string? labelFilter)
    {
        var all = provider.ListSubresourcesAs<AppConfigurationKeyValue>(sub, rg, storeName, KvSubresource);
        if (!string.IsNullOrEmpty(keyFilter) && keyFilter != "*")
            all = all.Where(kv => MatchesGlob(kv.Key, keyFilter)).ToArray();
        if (labelFilter != null)
        {
            var labels = labelFilter.Split(',', StringSplitOptions.RemoveEmptyEntries);
            all = all.Where(kv => labels.Any(l =>
                l == "\0" || l == "\u0000" ? kv.Label == null : string.Equals(kv.Label, l, StringComparison.Ordinal))).ToArray();
        }
        return all;
    }

    public AppConfigurationKeyValue SetKv(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string storeName, string key, string? label, string? value, string? contentType, Dictionary<string, string>? tags)
    {
        var id = AppConfigurationKeyValue.ToFileId(key, label);
        var existing = provider.GetSubresourceAs<AppConfigurationKeyValue>(sub, rg, id, storeName, KvSubresource);
        if (existing != null)
        {
            existing.Update(value, contentType, tags);
            provider.CreateOrUpdateSubresource(sub, rg, id, storeName, KvSubresource, existing);
            return existing;
        }
        var kv = AppConfigurationKeyValue.Create(key, label, value, contentType, tags);
        provider.CreateOrUpdateSubresource(sub, rg, id, storeName, KvSubresource, kv);
        return kv;
    }

    public AppConfigurationKeyValue? DeleteKv(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string storeName, string key, string? label)
    {
        var id = AppConfigurationKeyValue.ToFileId(key, label);
        var existing = provider.GetSubresourceAs<AppConfigurationKeyValue>(sub, rg, id, storeName, KvSubresource);
        if (existing == null) return null;
        provider.DeleteSubresource(sub, rg, id, storeName, KvSubresource);
        return existing;
    }

    public AppConfigurationKeyValue? SetKvLock(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string storeName, string key, string? label, bool locked)
    {
        var id = AppConfigurationKeyValue.ToFileId(key, label);
        var existing = provider.GetSubresourceAs<AppConfigurationKeyValue>(sub, rg, id, storeName, KvSubresource);
        if (existing == null) return null;
        existing.Locked = locked;
        provider.CreateOrUpdateSubresource(sub, rg, id, storeName, KvSubresource, existing);
        return existing;
    }

    private static bool MatchesGlob(string value, string pattern)
    {
        if (pattern == "*") return true;
        if (!pattern.Contains('*')) return string.Equals(value, pattern, StringComparison.Ordinal);
        var parts = pattern.Split('*');
        var pos = 0;
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            var idx = value.IndexOf(part, pos, StringComparison.Ordinal);
            if (idx < 0) return false;
            pos = idx + part.Length;
        }
        return true;
    }
}
