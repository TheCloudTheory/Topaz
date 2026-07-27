using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.Redis.Models;
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
                return new ControlPlaneOperationResult<RedisResource>(OperationResult.Updated, updated, null, null);
            }

            provider.CreateOrUpdate(sub, rg, name, existing);
            return new ControlPlaneOperationResult<RedisResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? rgOp.Resource!.Location!;
        var resource = new RedisResource(sub, rg, name, location, request.Tags, request.Sku, request.Properties);

        provider.CreateOrUpdate(sub, rg, name, resource, createOperation: true);

        return new ControlPlaneOperationResult<RedisResource>(OperationResult.Created, resource, null, null);
    }
}