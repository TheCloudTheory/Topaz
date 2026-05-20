using Azure.Core;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.AppService.Models;
using Topaz.Service.AppService.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Service.Subscription;

namespace Topaz.Service.AppService;

internal sealed class AppServicePlanControlPlane(
    AppServicePlanResourceProvider provider,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane = subscriptionControlPlane;
    private const string NotFoundCode = "AppServicePlanNotFound";
    private const string NotFoundMessageTemplate = "App Service Plan '{0}' could not be found";

    public static AppServicePlanControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(new AppServicePlanResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public ControlPlaneOperationResult<AppServicePlanResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string planName,
        CreateOrUpdateAppServicePlanRequest request)
    {
        var existing = provider.GetAs<AppServicePlanResource>(subscriptionIdentifier, resourceGroupIdentifier, planName);

        var location = !string.IsNullOrWhiteSpace(request.Location)
            ? new AzureLocation(request.Location)
            : AzureLocation.WestEurope;

        ResourceSku? sku = null;
        if (request.Sku != null)
            sku = new ResourceSku { Name = request.Sku.Name, Family = request.Sku.Family };

        AppServicePlanResource resource;
        bool isCreate;

        if (existing == null)
        {
            isCreate = true;
            var properties = AppServicePlanResourceProperties.FromRequest(request);
            resource = new AppServicePlanResource(subscriptionIdentifier, resourceGroupIdentifier, planName, location,
                request.Tags, sku, properties);
        }
        else
        {
            isCreate = false;
            AppServicePlanResourceProperties.UpdateFromRequest(existing, request);
            resource = existing;
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, planName, resource, isCreate);

        return new ControlPlaneOperationResult<AppServicePlanResource>(
            isCreate ? OperationResult.Created : OperationResult.Updated, resource, null, null);
    }

    public ControlPlaneOperationResult<AppServicePlanResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string planName)
    {
        var resource = provider.GetAs<AppServicePlanResource>(subscriptionIdentifier, resourceGroupIdentifier, planName);
        return resource == null
            ? new ControlPlaneOperationResult<AppServicePlanResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, planName), NotFoundCode)
            : new ControlPlaneOperationResult<AppServicePlanResource>(OperationResult.Success, resource, null, null);
    }

    public OperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string planName)
    {
        var existing = provider.GetAs<AppServicePlanResource>(subscriptionIdentifier, resourceGroupIdentifier, planName);
        if (existing == null) return OperationResult.NotFound;

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, planName);
        return OperationResult.Success;
    }

    public ControlPlaneOperationResult<AppServicePlanResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<AppServicePlanResource>(subscriptionIdentifier, resourceGroupIdentifier, null, null);
        return new ControlPlaneOperationResult<AppServicePlanResource[]>(
            OperationResult.Success, resources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<AppServicePlanResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<AppServicePlanResource>(subscriptionIdentifier, null, null, null);
        return new ControlPlaneOperationResult<AppServicePlanResource[]>(
            OperationResult.Success,
            resources.Where(r => r.IsInSubscription(subscriptionIdentifier)).ToArray(),
            null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        var plan = resource.As<AppServicePlanResource, AppServicePlanResourceProperties>();
        if (plan == null)
        {
            logger.LogError($"Could not parse generic resource '{resource.Id}' as an App Service Plan.");
            return OperationResult.Failed;
        }

        try
        {
            var skuDescription = plan.Sku == null
                ? null
                : new CreateOrUpdateAppServicePlanRequest.AppServicePlanSkuDescription
                {
                    Name = plan.Sku.Name,
                    Family = plan.Sku.Family,
                };

            var result = CreateOrUpdate(
                plan.GetSubscription(),
                plan.GetResourceGroup(),
                plan.Name,
                new CreateOrUpdateAppServicePlanRequest
                {
                    Location = plan.Location,
                    Tags = plan.Tags,
                    Sku = skuDescription,
                    Properties = new CreateOrUpdateAppServicePlanRequest.AppServicePlanProperties
                    {
                        NumberOfWorkers = plan.Properties.NumberOfWorkers,
                        MaximumNumberOfWorkers = plan.Properties.MaximumNumberOfWorkers,
                        WorkerTierName = plan.Properties.WorkerTierName,
                        HyperV = plan.Properties.HyperV,
                        IsSpot = plan.Properties.IsSpot,
                    },
                });

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
}
