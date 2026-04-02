using System.Text.Json;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public abstract class ArmResource<T>
{
    public abstract string Id { get; init; }
    public abstract string Name { get; init; }
    public abstract string Type { get; init; }
    public abstract string? Location { get; set; }
    public abstract IDictionary<string, string>? Tags { get; set; }
    public abstract ResourceSku? Sku { get; init; }
    public abstract string? Kind { get; init; }
    public abstract T Properties { get; init; }
    public virtual ResourceIdentity? Identity { get; set; }

    public SubscriptionIdentifier GetSubscription()
    {
        return SubscriptionIdentifier.From(Guid.Parse(Id.Split("/")[2]));
    }
    
    public ResourceGroupIdentifier GetResourceGroup()
    {
        return ResourceGroupIdentifier.From(Id.Split("/")[4]);
    }

    public bool IsInSubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        if (Id is null) return false;
        var segments = Id.Split("/");
        return segments.Length > 2 && segments[2] == subscriptionIdentifier.Value.ToString();
    }

    public bool IsInResourceGroup(ResourceGroupIdentifier resourceGroupIdentifier)
    {
        if (Id is null) return false;
        var segments = Id.Split("/");
        return segments.Length > 4 && segments[4] == resourceGroupIdentifier.Value;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}