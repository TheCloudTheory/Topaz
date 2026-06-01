using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class CosmosDbServiceControlPlane(
    Pipeline eventPipeline,
    DatabaseAccountResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string DatabaseAccountNotFoundCode = "DatabaseAccountNotFound";
    private const string DatabaseAccountNotFoundMessageTemplate = "Database account '{0}' could not be found.";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static CosmosDbServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new DatabaseAccountResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var account = resource.As<DatabaseAccountResource, DatabaseAccountResourceProperties>();
        if (account == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Cosmos DB database account.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(account.Location))
        {
            logger.LogError($"Cosmos DB database account resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                account.GetSubscription(),
                account.GetResourceGroup(),
                account.Name,
                CreateOrUpdateDatabaseAccountRequest.FromResource(account));

            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }

    public ControlPlaneOperationResult<DatabaseAccountResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName)
    {
        var resource = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        return resource == null
            ? new ControlPlaneOperationResult<DatabaseAccountResource>(
                OperationResult.NotFound,
                null,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode)
            : new ControlPlaneOperationResult<DatabaseAccountResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<DatabaseAccountResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        CreateOrUpdateDatabaseAccountRequest request)
    {
        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<DatabaseAccountResource>(
                OperationResult.NotFound,
                null,
                resourceGroupOperation.Reason,
                resourceGroupOperation.Code);
        }

        var existing = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        if (existing != null)
        {
            existing.Location = request.Location?.ToString() ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            DatabaseAccountResourceProperties.UpdateFromRequest(existing.Properties, request);
            provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, accountName, existing);

            return new ControlPlaneOperationResult<DatabaseAccountResource>(OperationResult.Updated, existing, null, null);
        }

        var location = new AzureLocation(request.Location ?? resourceGroupOperation.Resource!.Location!);
        var properties = DatabaseAccountResourceProperties.FromRequest(accountName, request);
        var resource = new DatabaseAccountResource(subscriptionIdentifier, resourceGroupIdentifier, accountName, location, request.Tags, properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, accountName, resource, createOperation: true);

        return new ControlPlaneOperationResult<DatabaseAccountResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName)
    {
        var resource = provider.GetAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        if (resource == null)
        {
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(DatabaseAccountNotFoundMessageTemplate, accountName),
                DatabaseAccountNotFoundCode);
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, accountName);

        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<DatabaseAccountResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<DatabaseAccountResource>(subscriptionIdentifier, resourceGroupIdentifier,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier) && r.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<DatabaseAccountResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<DatabaseAccountResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<DatabaseAccountResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<DatabaseAccountResource[]>(OperationResult.Success, resources, null, null);
    }
}
