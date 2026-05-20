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

internal sealed class AppServiceSiteControlPlane(
    AppServiceSiteResourceProvider provider,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger) : IControlPlane
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane = subscriptionControlPlane;
    private const string NotFoundCode = "AppServiceSiteNotFound";
    private const string NotFoundMessageTemplate = "App Service Site '{0}' could not be found";

    public static AppServiceSiteControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(new AppServiceSiteResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public ControlPlaneOperationResult<AppServiceSiteResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName,
        CreateOrUpdateAppServiceSiteRequest request)
    {
        var existing = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);

        var location = !string.IsNullOrWhiteSpace(request.Location)
            ? new AzureLocation(request.Location)
            : AzureLocation.WestEurope;

        AppServiceSiteResource resource;
        bool isCreate;

        if (existing == null)
        {
            isCreate = true;
            var properties = AppServiceSiteResourceProperties.FromRequest(siteName, request);
            resource = new AppServiceSiteResource(subscriptionIdentifier, resourceGroupIdentifier, siteName, location,
                request.Tags, request.Kind, properties);
        }
        else
        {
            isCreate = false;
            AppServiceSiteResourceProperties.UpdateFromRequest(existing, request);
            resource = existing;
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, siteName, resource, isCreate);

        return new ControlPlaneOperationResult<AppServiceSiteResource>(
            isCreate ? OperationResult.Created : OperationResult.Updated, resource, null, null);
    }

    public ControlPlaneOperationResult<AppServiceSiteResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName)
    {
        var resource = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        return resource == null
            ? new ControlPlaneOperationResult<AppServiceSiteResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode)
            : new ControlPlaneOperationResult<AppServiceSiteResource>(OperationResult.Success, resource, null, null);
    }

    public OperationResult Delete(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName)
    {
        var existing = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        if (existing == null) return OperationResult.NotFound;

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        return OperationResult.Success;
    }

    public ControlPlaneOperationResult<AppServiceSiteResource[]> ListByResourceGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, null, null);
        return new ControlPlaneOperationResult<AppServiceSiteResource[]>(
            OperationResult.Success, resources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<AppServiceSiteResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<AppServiceSiteResource>(subscriptionIdentifier, null, null, null);
        return new ControlPlaneOperationResult<AppServiceSiteResource[]>(
            OperationResult.Success,
            resources.Where(r => r.IsInSubscription(subscriptionIdentifier)).ToArray(),
            null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        var site = resource.As<AppServiceSiteResource, AppServiceSiteResourceProperties>();
        if (site == null)
        {
            logger.LogError($"Could not parse generic resource '{resource.Id}' as an App Service Site.");
            return OperationResult.Failed;
        }

        try
        {
            var result = CreateOrUpdate(
                site.GetSubscription(),
                site.GetResourceGroup(),
                site.Name,
                new CreateOrUpdateAppServiceSiteRequest
                {
                    Location = site.Location,
                    Tags = site.Tags,
                    Kind = site.Kind,
                    Properties = new CreateOrUpdateAppServiceSiteRequest.AppServiceSiteProperties
                    {
                        ServerFarmId = site.Properties.ServerFarmId,
                        SiteConfig = site.Properties.SiteConfig == null
                            ? null
                            : new CreateOrUpdateAppServiceSiteRequest.SiteConfigRequest
                            {
                                AppSettings = site.Properties.SiteConfig.AppSettings,
                                ConnectionStrings = site.Properties.SiteConfig.ConnectionStrings,
                                LinuxFxVersion = site.Properties.SiteConfig.LinuxFxVersion,
                                NetFrameworkVersion = site.Properties.SiteConfig.NetFrameworkVersion,
                                AlwaysOn = site.Properties.SiteConfig.AlwaysOn,
                                FtpsState = site.Properties.SiteConfig.FtpsState,
                                MinTlsVersion = site.Properties.SiteConfig.MinTlsVersion,
                            },
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
