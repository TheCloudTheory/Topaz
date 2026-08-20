using System.Text.Json;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public abstract class ArmSubresource<T>
{
    public abstract string Id { get; init; }
    public abstract string Name { get; init; }
    public abstract string Type { get; init; }
    public abstract T Properties { get; init; }
    
    public SubscriptionIdentifier GetSubscription()
    {
        return SubscriptionIdentifier.From(Guid.Parse(Id.Split("/")[2]));
    }
    
    public ResourceGroupIdentifier GetResourceGroup()
    {
        return ResourceGroupIdentifier.From(Id.Split("/")[4]);
    }

    /// <summary>
    /// Retrieves the last segment of the resource name, which typically represents
    /// the unique identifier for the resource within its hierarchy.
    /// </summary>
    /// <returns>
    /// A string containing the last segment of the resource name.
    /// </returns>
    public string GetName()
    {
        var segments = Name.Split('/');
        return segments[^1];
    }
    
    public string GetParentId()
    {
        return Id.Split("/")[9];
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GetType(), GlobalSettings.JsonOptions);
    }
}