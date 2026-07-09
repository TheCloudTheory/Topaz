using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.LogAnalytics.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics;

internal sealed class LogAnalyticsServiceControlPlane(
    Pipeline eventPipeline,
    WorkspaceResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "Workspace '{0}' could not be found";

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public static LogAnalyticsServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new WorkspaceResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var workspace = resource.As<WorkspaceResource, WorkspaceResourceProperties>();
        if (workspace == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a WorkspaceResource instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(workspace.Location))
        {
            logger.LogError($"WorkspaceResource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(workspace.GetSubscription(), workspace.GetResourceGroup(), workspace.Name, workspace);
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

    public ControlPlaneOperationResult<WorkspaceResource> CreateOrUpdate(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        WorkspaceResource request)
    {
        var rgOp = _resourceGroupControlPlane.Get(sub, rg);
        if (rgOp.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<WorkspaceResource>(
                OperationResult.NotFound, null, rgOp.Reason, rgOp.Code);

        var existing = provider.GetAs<WorkspaceResource>(sub, rg, name);

        if (existing != null)
        {
            existing.Location = request.Location ?? existing.Location;
            existing.Tags = request.Tags ?? existing.Tags;
            if (request.Properties?.RetentionInDays > 0)
                existing.Properties.RetentionInDays = request.Properties.RetentionInDays;
            if (request.Properties?.Sku != null)
                existing.Properties.Sku = request.Properties.Sku;
            if (request.Properties?.PublicNetworkAccessForIngestion != null)
                existing.Properties.PublicNetworkAccessForIngestion = request.Properties.PublicNetworkAccessForIngestion;
            if (request.Properties?.PublicNetworkAccessForQuery != null)
                existing.Properties.PublicNetworkAccessForQuery = request.Properties.PublicNetworkAccessForQuery;

            provider.CreateOrUpdate(sub, rg, name, existing);
            return new ControlPlaneOperationResult<WorkspaceResource>(OperationResult.Updated, existing, null, null);
        }

        var location = request.Location ?? rgOp.Resource!.Location!;
        var properties = WorkspaceResourceProperties.FromRequest(request.Properties);
        var resource = new WorkspaceResource(sub, rg, name, location, request.Tags, properties);

        provider.CreateOrUpdate(sub, rg, name, resource, createOperation: true);
        return new ControlPlaneOperationResult<WorkspaceResource>(OperationResult.Created, resource, null, null);
    }

    public ControlPlaneOperationResult<WorkspaceResource> Get(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<WorkspaceResource>(sub, rg, name);
        return resource == null
            ? new ControlPlaneOperationResult<WorkspaceResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode)
            : new ControlPlaneOperationResult<WorkspaceResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult Delete(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name)
    {
        var resource = provider.GetAs<WorkspaceResource>(sub, rg, name);
        if (resource == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound, string.Format(NotFoundMessage, name), NotFoundCode);

        provider.Delete(sub, rg, name);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<WorkspaceResource> Update(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg,
        string name,
        UpdateWorkspaceRequest request)
    {
        var existing = provider.GetAs<WorkspaceResource>(sub, rg, name);
        if (existing == null)
            return new ControlPlaneOperationResult<WorkspaceResource>(
                OperationResult.NotFound, null, string.Format(NotFoundMessage, name), NotFoundCode);

        if (request.Tags != null)
            existing.Tags = request.Tags;

        existing.Properties.UpdateFromRequest(request);
        provider.CreateOrUpdate(sub, rg, name, existing);
        return new ControlPlaneOperationResult<WorkspaceResource>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult<WorkspaceResource[]> ListByResourceGroup(
        SubscriptionIdentifier sub,
        ResourceGroupIdentifier rg)
    {
        var resources = provider.ListAs<WorkspaceResource>(sub, rg, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub) && r.IsInResourceGroup(rg))
            .ToArray();
        return new ControlPlaneOperationResult<WorkspaceResource[]>(OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<WorkspaceResource[]> ListBySubscription(
        SubscriptionIdentifier sub)
    {
        var resources = provider.ListAs<WorkspaceResource>(sub, null, lookForNoOfSegments: 8)
            .Where(r => r.IsInSubscription(sub))
            .ToArray();
        return new ControlPlaneOperationResult<WorkspaceResource[]>(OperationResult.Success, resources, null, null);
    }
}
