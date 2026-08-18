using System.Text.Json;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.AppConfiguration.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class ConfigurationStoreFullResource : ConfigurationStoreResource, IValidatable
{
    [UsedImplicitly]
    public ConfigurationStoreFullResource()
    {
    }

    public ConfigurationStoreFullResource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storeName,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        ConfigurationStoreResourceProperties properties) : base(subscriptionIdentifier, resourceGroupIdentifier,
        storeName, location, tags, sku, properties)
    {
    }

    public DateTimeOffset? DeletionDate { get; set; }
    public DateTimeOffset? ScheduledPurgeDate { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (data is not UpdateConfigurationStoreRequest request)
        {
            throw new InvalidOperationException();
        }

        if (Properties.EnablePurgeProtection is not null && Properties.EnablePurgeProtection.Value && !request.Properties!.EnablePurgeProtection!.Value)
        {
            return new ValueTuple<bool, string?>(false, "Purge protection can be changed after creation.");
        }

        if (request.Sku?.Name == "Free" && request.Properties!.EnablePurgeProtection.HasValue)
        {
            return new ValueTuple<bool, string?>(true, "Purge protection can't be enabled for Free SKU.");
        }
        
        return new ValueTuple<bool, string?>(true, null);
    }

    public void UpdateFromRequest(ConfigurationStoreResource request)
    {
        Tags = request.Tags ?? Tags;
        Sku = request.Sku ?? Sku;
        
        Properties.UpdateFromRequest(request, Sku!.Name!);
    }

    public void UpdateFromRequest(UpdateConfigurationStoreRequest request)
    {
        Tags =  request.Tags ?? Tags;
        Sku = request.Sku ?? Sku;
        
        Properties.UpdateFromRequest(request, Sku!.Name!);
    }
}