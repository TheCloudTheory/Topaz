using System.Text.Json;
using System.Xml.Serialization;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public abstract class ArmSubresource<T>
{
    public abstract string Id { get; init; }
    public abstract string Name { get; init; }
    public abstract string Type { get; }
    public abstract T Properties { get; init; }
    
    public SubscriptionIdentifier GetSubscription()
    {
        return SubscriptionIdentifier.From(Guid.Parse(Id.Split("/")[2]));
    }
    
    public ResourceGroupIdentifier GetResourceGroup()
    {
        return ResourceGroupIdentifier.From(Id.Split("/")[4]);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}