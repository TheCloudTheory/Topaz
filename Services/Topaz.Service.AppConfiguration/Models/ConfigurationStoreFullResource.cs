using System.Text.Json;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class ConfigurationStoreFullResource : ConfigurationStoreResource
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
}