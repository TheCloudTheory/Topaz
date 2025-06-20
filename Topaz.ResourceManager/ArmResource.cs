using System.Text.Json;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public abstract class ArmResource<T>
{
    public abstract string Id { get; init; }
    public abstract string Name { get; init; }
    public abstract string Type { get; }
    public abstract string Location { get; init; }
    public abstract IDictionary<string, string> Tags { get; }
    public abstract ResourceSku? Sku { get; init; }
    public abstract string? Kind { get; init; }
    public abstract T Properties { get; init; }

    public SubscriptionIdentifier GetSubscription()
    {
        return SubscriptionIdentifier.From(Guid.Parse(Id.Split("/")[2]));
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}