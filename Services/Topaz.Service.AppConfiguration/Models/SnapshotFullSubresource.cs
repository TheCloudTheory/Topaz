using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class SnapshotFullSubresource : SnapshotSubresource
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SnapshotFullSubresource()
#pragma warning restore CS8618
    {
    }

    public SnapshotFullSubresource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        SnapshotSubresourceProperties properties) : base(subscriptionId, resourceGroup, name, properties)
    {
        LastModified = DateTimeOffset.UtcNow;
        SyncToken = GenerateSyncToken();
    }

    private string GenerateSyncToken()
    {
        return "topaz=MA==;sn=1";
    }

    public DateTimeOffset? LastModified { get; set; }
    public string? SyncToken { get; set; }
    
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}