using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.Insights.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class ApplicationInsightsServiceControlPlane(
    Pipeline eventPipeline,
    ApplicationInsightsResourceProvider provider,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "Application Insights component '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static ApplicationInsightsServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ApplicationInsightsResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var component = resource.As<ApplicationInsightsComponentResource, ApplicationInsightsComponentResourceProperties>();
        if (component == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as an ApplicationInsightsComponentResource instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(component.Location))
        {
            logger.LogError($"ApplicationInsightsComponentResource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(component.GetSubscription(), component.GetResourceGroup(), component.Name, component);
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

    public ControlPlaneOperationResult<ApplicationInsightsComponentResource> CreateOrUpdate(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        ApplicationInsightsComponentResource request)
    {
        var rgOp = _resourceGroupControlPlane.Get(sub, rg);
        if (rgOp.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(
                OperationResult.NotFound, null, rgOp.Reason, rgOp.Code);

        var existing = provider.GetAs<ApplicationInsightsComponentResource>(sub, rg, name);

        if (existing != null)
        {
            existing.Location = request.Location ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            if (request.Properties?.ApplicationType != null)
                existing.Properties.ApplicationType = request.Properties.ApplicationType;
            if (request.Properties?.IngestionMode != null)
                existing.Properties.IngestionMode = request.Properties.IngestionMode;

            provider.CreateOrUpdate(sub, rg, name, existing);
            return new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? rgOp.Resource!.Location!;
        var properties = ApplicationInsightsComponentResourceProperties.FromRequest(
            request.Properties, name, GlobalSettings.DefaultApplicationInsightsPort);
        var resource = new ApplicationInsightsComponentResource(sub, rg, name, location, request.Tags, request.Kind, properties);

        provider.CreateOrUpdate(sub, rg, name, resource, createOperation: true);
        return new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<ApplicationInsightsComponentResource> Get(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ApplicationInsightsComponentResource>(sub, rg, name);
        return resource == null
            ? new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<ApplicationInsightsComponentResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, string.Format(NotFoundMessage, name), NotFoundCode);

        provider.Delete(sub, rg, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<ApplicationInsightsComponentResource> Update(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        UpdateComponentRequest request)
    {
        var existing = provider.GetAs<ApplicationInsightsComponentResource>(sub, rg, name);
        if (existing == null)
            return new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        if (request.Tags != null)
            existing.Tags = request.Tags;
        if (request.Properties?.RetentionInDays.HasValue == true)
            existing.Properties.RetentionInDays = request.Properties.RetentionInDays.Value;
        if (request.Properties?.PublicNetworkAccessForIngestion != null)
            existing.Properties.PublicNetworkAccessForIngestion = request.Properties.PublicNetworkAccessForIngestion;

        provider.CreateOrUpdate(sub, rg, name, existing);
        return new ControlPlaneOperationResult<ApplicationInsightsComponentResource>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult<ApplicationInsightsComponentResource[]> ListByResourceGroup(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg)
    {
        var resources = provider.ListAs<ApplicationInsightsComponentResource>(sub, rg, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub) && r.IsInResourceGroup(rg))
            .ToArray();
        return new ControlPlaneOperationResult<ApplicationInsightsComponentResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ApplicationInsightsComponentResource[]> ListBySubscription(
        SubscriptionIdentifier sub)
    {
        var resources = provider.ListAs<ApplicationInsightsComponentResource>(sub, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub))
            .ToArray();
        return new ControlPlaneOperationResult<ApplicationInsightsComponentResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ApplicationInsightsComponentResource?> GetByInstrumentationKey(
        string instrumentationKey)
    {
        var subscriptions = subscriptionControlPlane.List();
        if (subscriptions.Result != OperationResult.Success || subscriptions.Resource == null ||
            subscriptions.Resource.Length == 0)
        {
            return new ControlPlaneOperationResult<ApplicationInsightsComponentResource?>(OperationResult.Failed, null,"No subscriptions found",
                "NoSubscriptionsFound");
        }
        
        var components = new List<ApplicationInsightsComponentResource>();
        foreach (var subscription in subscriptions.Resource)
        {
            var componentsInSubscription =
                ListBySubscription(SubscriptionIdentifier.From(subscription.SubscriptionId));
            if (componentsInSubscription.Result != OperationResult.Success ||
                componentsInSubscription.Resource == null || componentsInSubscription.Resource.Length == 0)
            {
                continue;
            }

            components.AddRange(componentsInSubscription.Resource);
        }
        
        if (components.Count == 0 || components.All(w => w.Properties.InstrumentationKey != instrumentationKey))
        {
            return new ControlPlaneOperationResult<ApplicationInsightsComponentResource?>(OperationResult.Failed, null, "No resources found", "NoComponentsFound");
        }
        
        var component = components.SingleOrDefault(w => w.Properties.InstrumentationKey == instrumentationKey)!;
        return new ControlPlaneOperationResult<ApplicationInsightsComponentResource?>(OperationResult.Updated, component, null, null);
    }
}
