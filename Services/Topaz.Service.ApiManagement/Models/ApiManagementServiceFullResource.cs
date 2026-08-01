using System.Text.Json;
using JetBrains.Annotations;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiManagementServiceFullResource : ApiManagementServiceResource
{
    [UsedImplicitly]
    public ApiManagementServiceFullResource()
    {
    }

    public ApiManagementServiceFullResource(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        ApiManagementServiceResourceProperties properties) : base(subscriptionIdentifier, resourceGroupIdentifier, name, location, tags, sku, properties)
    {
    }

    public DateTimeOffset? DeletionDate  { get; set; }
    public DateTimeOffset? ScheduledPurgeDate  { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}