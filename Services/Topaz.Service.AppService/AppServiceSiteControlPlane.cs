using System.Reflection;
using Azure.Core;
using Topaz.Dns;
using Topaz.ResourceManager;
using Topaz.Service.AppService.Models;
using Topaz.Service.AppService.Models.Requests;
using Topaz.Service.AppService.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppService;

internal sealed class AppServiceSiteControlPlane(
    AppServiceSiteResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "AppServiceSiteNotFound";
    private const string NotFoundMessageTemplate = "App Service Site '{0}' could not be found";

    public static AppServiceSiteControlPlane New(ITopazLogger logger) =>
        new(new AppServiceSiteResourceProvider(logger),  logger);

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
                request.Tags, request.Kind ?? "app", properties);
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

    public ControlPlaneOperationResult<AppServiceSiteConfigResource> GetSiteConfig(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName)
    {
        var resource = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        return resource == null
            ? new ControlPlaneOperationResult<AppServiceSiteConfigResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode)
            : new ControlPlaneOperationResult<AppServiceSiteConfigResource>(OperationResult.Success,
                AppServiceSiteConfigResource.FromSite(resource), null, null);
    }
    
    public ControlPlaneOperationResult<AppServiceSiteConfigResource> GetSiteConfig(string siteName)
    {
        var dnsEntry = GlobalDnsEntries.GetEntry(AppServiceSiteService.UniqueName, siteName);
        if (dnsEntry == null)
            return new ControlPlaneOperationResult<AppServiceSiteConfigResource>(
                OperationResult.NotFound, null, null, null);

        var existingSubId = SubscriptionIdentifier.From(dnsEntry.Value.subscription);
        var existingRgId = dnsEntry.Value.resourceGroup != null
            ? ResourceGroupIdentifier.From(dnsEntry.Value.resourceGroup)
            : null;

        if (existingRgId == null)
            return new ControlPlaneOperationResult<AppServiceSiteConfigResource>(
                OperationResult.NotFound, null, null, null);
        
        var resource = GetSiteConfig(existingSubId, existingRgId, siteName);
        return resource.Resource == null
            ? new ControlPlaneOperationResult<AppServiceSiteConfigResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode)
            : new ControlPlaneOperationResult<AppServiceSiteConfigResource>(OperationResult.Success,
                resource.Resource, null, null);
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
        var resources = provider.ListAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);
        return new ControlPlaneOperationResult<AppServiceSiteResource[]>(
            OperationResult.Success, resources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<AppServiceSiteResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<AppServiceSiteResource>(subscriptionIdentifier, null, null, 8);
        return new ControlPlaneOperationResult<AppServiceSiteResource[]>(
            OperationResult.Success,
            resources.Where(r => r.IsInSubscription(subscriptionIdentifier)).ToArray(),
            null, null);
    }

    public ControlPlaneOperationResult<CheckAppServiceSiteNameResponse> CheckNameAvailability(string siteName)
    {
        var dnsEntry = GlobalDnsEntries.GetEntry(AppServiceSiteService.UniqueName, siteName);
        if (dnsEntry == null)
            return new ControlPlaneOperationResult<CheckAppServiceSiteNameResponse>(
                OperationResult.Success, new CheckAppServiceSiteNameResponse { NameAvailable = true }, null, null);

        var existingSubId = SubscriptionIdentifier.From(dnsEntry.Value.subscription);
        var existingRgId = dnsEntry.Value.resourceGroup != null
            ? ResourceGroupIdentifier.From(dnsEntry.Value.resourceGroup)
            : null;

        if (existingRgId == null)
            return new ControlPlaneOperationResult<CheckAppServiceSiteNameResponse>(
                OperationResult.Success, new CheckAppServiceSiteNameResponse { NameAvailable = true }, null, null);

        var existing = provider.GetAs<AppServiceSiteResource>(existingSubId, existingRgId, siteName);
        if (existing == null)
            return new ControlPlaneOperationResult<CheckAppServiceSiteNameResponse>(
                OperationResult.Success, new CheckAppServiceSiteNameResponse { NameAvailable = true }, null, null);

        return new ControlPlaneOperationResult<CheckAppServiceSiteNameResponse>(
            OperationResult.Success,
            new CheckAppServiceSiteNameResponse
            {
                NameAvailable = false,
                Reason = CheckAppServiceSiteNameResponse.NoAvailabilityReason.AlreadyExists,
                Message = $"The name '{siteName}' is already in use."
            },
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

    public ControlPlaneOperationResult<AppServiceSiteConfigResource> UpdateSiteConfig(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName,
        UpdateSiteConfigWebRequest request)
    {
        var resource = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        if (resource == null)
            return new ControlPlaneOperationResult<AppServiceSiteConfigResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode);

        var props = request.Properties;
        if (props != null)
        {
            var siteConfig = resource.Properties.SiteConfig ??= new SiteConfigProperties();
            if (props.AppSettings != null) siteConfig.AppSettings = props.AppSettings;
            if (props.ConnectionStrings != null) siteConfig.ConnectionStrings = props.ConnectionStrings;
            if (props.LinuxFxVersion != null) siteConfig.LinuxFxVersion = props.LinuxFxVersion;
            if (props.NetFrameworkVersion != null) siteConfig.NetFrameworkVersion = props.NetFrameworkVersion;
            if (props.AlwaysOn.HasValue) siteConfig.AlwaysOn = props.AlwaysOn.Value;
            if (props.FtpsState != null) siteConfig.FtpsState = props.FtpsState;
            if (props.MinTlsVersion != null) siteConfig.MinTlsVersion = props.MinTlsVersion;
        }

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, siteName, resource, false);
        return new ControlPlaneOperationResult<AppServiceSiteConfigResource>(
            OperationResult.Updated, AppServiceSiteConfigResource.FromSite(resource), null, null);
    }

    public ControlPlaneOperationResult<AppServiceAppSettingsConfigResource> UpdateAppSettings(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName,
        Dictionary<string, string> settings)
    {
        var resource = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        if (resource == null)
            return new ControlPlaneOperationResult<AppServiceAppSettingsConfigResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode);

        var siteConfig = resource.Properties.SiteConfig ??= new SiteConfigProperties();
        siteConfig.AppSettings = settings
            .Select(pair => new AppServiceNameValuePair { Name = pair.Key, Value = pair.Value })
            .ToArray();

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, siteName, resource, false);
        return new ControlPlaneOperationResult<AppServiceAppSettingsConfigResource>(
            OperationResult.Updated, AppServiceAppSettingsConfigResource.FromSite(resource), null, null);
    }

    public ControlPlaneOperationResult<AppServiceAppSettingsConfigResource> GetAppSettings(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName)
    {
        var resource = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        return resource == null
            ? new ControlPlaneOperationResult<AppServiceAppSettingsConfigResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode)
            : new ControlPlaneOperationResult<AppServiceAppSettingsConfigResource>(
                OperationResult.Success, AppServiceAppSettingsConfigResource.FromSite(resource), null, null);
    }

    public ControlPlaneOperationResult<AppServiceSlotConfigNamesResource> GetSlotConfigNames(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string siteName)
    {
        var resource = provider.GetAs<AppServiceSiteResource>(subscriptionIdentifier, resourceGroupIdentifier, siteName);
        return resource == null
            ? new ControlPlaneOperationResult<AppServiceSlotConfigNamesResource>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, siteName), NotFoundCode)
            : new ControlPlaneOperationResult<AppServiceSlotConfigNamesResource>(
                OperationResult.Success, AppServiceSlotConfigNamesResource.FromSite(resource), null, null);
    }

    public ControlPlaneOperationResult<string> GetWebAppStacks()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Topaz.Service.AppService.Data.webAppStacks.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return new ControlPlaneOperationResult<string>(OperationResult.Success, reader.ReadToEnd(), null, null);
    }

    public (SubscriptionIdentifier Sub, ResourceGroupIdentifier Rg, AppServiceSiteResource Site)? FindSiteByName(
        string siteName) => provider.FindSiteByName(siteName);

    public string ZipDeploy(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string siteName, Stream body)
    {
        var id = Guid.NewGuid().ToString();
        provider.SaveDeploymentZip(sub, rg, siteName, id, body);
        provider.SaveDeploymentRecord(sub, rg, siteName, id, Models.DeploymentRecord.Succeeded(id));
        return id;
    }

    public Models.DeploymentRecord? GetDeployment(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string siteName, string id) =>
        provider.GetDeploymentRecord(sub, rg, siteName, id);

    public IReadOnlyList<Models.DeploymentRecord> ListDeployments(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string siteName) =>
        provider.ListDeploymentRecords(sub, rg, siteName);
}
