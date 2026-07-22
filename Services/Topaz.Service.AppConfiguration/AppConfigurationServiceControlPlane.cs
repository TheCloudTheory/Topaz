using Topaz.Dns;
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
    private const string ReplicaSubresource = "replicas";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static AppConfigurationServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new AppConfigurationResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var store = resource.As<ConfigurationStoreFullResource, ConfigurationStoreResourceProperties>();
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

    public ControlPlaneOperationResult<ConfigurationStoreFullResource> CreateOrUpdate(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        ConfigurationStoreResource request)
    {
        var rgOp = _resourceGroupControlPlane.Get(sub, rg);
        if (rgOp.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(
                OperationResult.NotFound, null, rgOp.Reason, rgOp.Code);

        var existing = provider.GetAs<ConfigurationStoreFullResource>(sub, rg, name);

        if (existing != null)
        {
            existing.Location = request.Location ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            if (request.Properties.PublicNetworkAccess != null)
                existing.Properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;

            if (request.Sku?.Name != null)
            {
                var updated = new ConfigurationStoreFullResource(
                    sub, rg, name, existing.Location!, existing.Tags,
                    new ResourceSku { Name = request.Sku.Name }, existing.Properties);
                provider.CreateOrUpdate(sub, rg, name, updated);
                return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(OperationResult.Updated, updated, null, null);
            }

            provider.CreateOrUpdate(sub, rg, name, existing);
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? rgOp.Resource!.Location!;
        var properties = ConfigurationStoreResourceProperties.FromRequest(request.Properties, name);
        var resource = new ConfigurationStoreFullResource(sub, rg, name, location, request.Tags, request.Sku, properties);

        provider.CreateOrUpdate(sub, rg, name, resource, createOperation: true);

        var keyStore = AppConfigurationAccessKeyStore.Generate(name);
        provider.CreateOrUpdateSubresource(sub, rg, AccessKeysId, name, AccessKeysSubresource, keyStore);

        return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreFullResource> Get(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ConfigurationStoreFullResource>(sub, rg, name);
        return resource == null || GlobalDnsEntries.IsSoftDeleted(AppConfigurationService.UniqueName, name)
            ? new ControlPlaneOperationResult<ConfigurationStoreFullResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<ConfigurationStoreFullResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ConfigurationStoreFullResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, string.Format(NotFoundMessage, name), NotFoundCode);

        provider.Delete(sub, rg, name, softDelete: true);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ConfigurationStoreFullResource> Update(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        UpdateConfigurationStoreRequest request)
    {
        var existing = provider.GetAs<ConfigurationStoreFullResource>(sub, rg, name);
        if (existing == null)
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        if (request.Tags != null)
            existing.Tags = request.Tags;
        if (request.Properties?.PublicNetworkAccess != null)
            existing.Properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;

        if (request.Sku?.Name != null)
        {
            var updated = new ConfigurationStoreFullResource(
                sub, rg, name, existing.Location!, existing.Tags,
                new ResourceSku { Name = request.Sku.Name }, existing.Properties);
            provider.CreateOrUpdate(sub, rg, name, updated);
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(OperationResult.Updated, updated, null, null);
        }

        provider.CreateOrUpdate(sub, rg, name, existing);
        return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreFullResource[]> ListByResourceGroup(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg)
    {
        var resources = provider.ListAs<ConfigurationStoreFullResource>(sub, rg, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub) && r.IsInResourceGroup(rg))
            .ToArray();
        return new ControlPlaneOperationResult<ConfigurationStoreFullResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ConfigurationStoreFullResource[]> ListBySubscription(
        SubscriptionIdentifier sub)
    {
        var resources = provider.ListAs<ConfigurationStoreFullResource>(sub, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub))
            .ToArray();
        
        return new ControlPlaneOperationResult<ConfigurationStoreFullResource[]>(OperationResult.Success, resources, null, null);
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
        var resource = provider.GetAs<ConfigurationStoreFullResource>(sub, rg, name);
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

    public ControlPlaneOperationResult<ConfigurationStoreFullResource> FindByName(string storeName)
    {
        var identifiers = GlobalDnsEntries.GetEntry(AppConfigurationService.UniqueName, storeName);
        if (identifiers == null)
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource>(
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
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "Listing KVs for store '{0}'...", storeName);
        var all = provider.ListSubresourcesAs<AppConfigurationKeyValue>(sub, rg, storeName, KvSubresource);
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "Found {0} KVs.", all.Length);
        
        if (!string.IsNullOrEmpty(keyFilter) && keyFilter != "*")
        {
            logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "keyFilter is not null, filtering KVs.");
            all = all.Where(kv => MatchesGlob(kv.Key, keyFilter)).ToArray();
        }
            
        if (labelFilter == null || labelFilter == "*")
        {
            logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "labelFilter is null, returning all KVs.");
            return all;
        }
        
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "labelFilter is not null, filtering KVs.");
        
        var labels = labelFilter.Split(',', StringSplitOptions.RemoveEmptyEntries);
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "Split labels: {0}", string.Join(", ", labels));
        
        all = all.Where(kv => labels.Any(l =>
            l is "\0" or $"\u0000" ? kv.Label == null : string.Equals(kv.Label, l, StringComparison.Ordinal))).ToArray();
        
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListKvs), "Filtered KVs: {0}", all.Length);
        
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

    public ControlPlaneOperationResult<ConfigurationStoreFullResource?> GetDeleted(SubscriptionIdentifier subscriptionIdentifier, string storeName)
    {
        var stores = ListBySubscription(subscriptionIdentifier).Resource;
        if (stores == null || stores.Length == 0)
        {
            logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(GetDeleted), $"No stores found for subscription {subscriptionIdentifier}");
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource?>(OperationResult.NotFound, null, null, null);
        }
        
        var store = stores.SingleOrDefault(s => s.Name == storeName && GlobalDnsEntries.IsSoftDeleted(AppConfigurationService.UniqueName, storeName));
        if (store != null)
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource?>(OperationResult.Success, store, null,
                null);
        
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(GetDeleted), $"No soft-deleted store found with name {storeName} for subscription {subscriptionIdentifier}");
        return new ControlPlaneOperationResult<ConfigurationStoreFullResource?>(OperationResult.NotFound, null, null, null);

    }

    public ControlPlaneOperationResult Purge(SubscriptionIdentifier subscriptionIdentifier, string storeName)
    {
        var deleted = GetDeleted(subscriptionIdentifier, storeName);
        if (deleted.Result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(GetDeleted), $"No soft-deleted store found with name {storeName} for subscription {subscriptionIdentifier}");
            return new ControlPlaneOperationResult(OperationResult.NotFound);
        }
        
        provider.Delete(subscriptionIdentifier, deleted.Resource!.GetResourceGroup(), storeName, softDelete: false);
        return new ControlPlaneOperationResult(OperationResult.Purged);
    }

    public ControlPlaneOperationResult<ConfigurationStoreFullResource[]?> ListDeleted(SubscriptionIdentifier subscriptionIdentifier)
    {
        var stores = ListBySubscription(subscriptionIdentifier).Resource;
        if (stores == null)
        {
            logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(ListDeleted), $"No stores found for subscription {subscriptionIdentifier}");
            return new ControlPlaneOperationResult<ConfigurationStoreFullResource[]?>(OperationResult.Success, null, null, null);
        }

        var deletedStores = stores
            .Where(store => GlobalDnsEntries.IsSoftDeleted(AppConfigurationService.UniqueName, store.Name)).ToArray();
        
        return new ControlPlaneOperationResult<ConfigurationStoreFullResource[]?>(OperationResult.Success, deletedStores, null, null);
    }

    public ControlPlaneOperationResult<ReplicaResource?> CreateReplica(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storeName, string replicaName, string location)
    {
        var store = Get(subscriptionIdentifier, resourceGroupIdentifier, storeName);
        if (store.Resource == null)
        {
            return new ControlPlaneOperationResult<ReplicaResource?>(OperationResult.NotFound, null, $"Store {storeName} not found", "StoreNotFound");
        }
        
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(CreateReplica), "Creating replica {0} for store {1}", replicaName, storeName);
        
        var replica = new ReplicaResource(subscriptionIdentifier, resourceGroupIdentifier, replicaName, location, null, ReplicaResourceProperties.From(replicaName, store.Resource));
        var (isValid, validationError) = replica.Validate();
        if (!isValid)
        {
            return new ControlPlaneOperationResult<ReplicaResource?>(OperationResult.Failed, null, validationError,
                "InvalidReplicaName");
        }
        
        provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, replicaName, storeName, ReplicaSubresource, replica);
        return new ControlPlaneOperationResult<ReplicaResource?>(OperationResult.Created, replica, null, null);
    }

    public ControlPlaneOperationResult<ReplicaResource?> GetReplica(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storeName, string replicaName)
    {
        var store = Get(subscriptionIdentifier, resourceGroupIdentifier, storeName);
        if (store.Resource == null)
        {
            return new ControlPlaneOperationResult<ReplicaResource?>(OperationResult.NotFound, null,
                $"Store {storeName} not found", "StoreNotFound");
        }

        var replica = provider.GetSubresourceAs<ReplicaResource>(subscriptionIdentifier, resourceGroupIdentifier,
            replicaName, storeName, ReplicaSubresource);
        
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(GetReplica), $"Loaded replica {replica?.Id} for store {storeName}");
        
        return replica == null
            ? new ControlPlaneOperationResult<ReplicaResource?>(OperationResult.NotFound, null,
                $"Replica {replicaName} not found", "ReplicaNotFound")
            : new ControlPlaneOperationResult<ReplicaResource?>(OperationResult.Success, replica, null, null);
    }

    public ControlPlaneOperationResult DeleteReplica(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storeName, string replicaName)
    {
        var store = Get(subscriptionIdentifier, resourceGroupIdentifier, storeName);
        if (store.Resource == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, $"Store {storeName} not found", "StoreNotFound");
        }

        var replica = provider.GetSubresourceAs<ReplicaResource>(subscriptionIdentifier, resourceGroupIdentifier,
            replicaName, storeName, ReplicaSubresource);
        
        logger.LogDebug(nameof(AppConfigurationServiceControlPlane), nameof(GetReplica), $"Loaded replica {replica?.Id} for store {storeName}");

        if (replica == null)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, $"Replica {replicaName} not found", "ReplicaNotFound");
        }
        
        provider.DeleteSubresource(subscriptionIdentifier, resourceGroupIdentifier, replicaName, storeName, ReplicaSubresource);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ReplicaResource[]?> ListReplicas(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storeName)
    {
        var store = Get(subscriptionIdentifier, resourceGroupIdentifier, storeName);
        if (store.Resource == null)
        {
            return new ControlPlaneOperationResult<ReplicaResource[]?>(OperationResult.NotFound, null, $"Store {storeName} not found", "StoreNotFound");
        }
        
        var replicas = provider.ListSubresourcesAs<ReplicaResource>(subscriptionIdentifier, resourceGroupIdentifier, storeName, ReplicaSubresource);
        return new ControlPlaneOperationResult<ReplicaResource[]?>(OperationResult.Success, replicas, null, null);
    }
}
