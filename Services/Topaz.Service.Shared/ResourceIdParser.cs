using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Shared;

public sealed class ResourceIdParser(string resourceId)
{
    private readonly string[] _segments = resourceId.Split('/');
    
    public SubscriptionIdentifier SubscriptionIdentifier => SubscriptionIdentifier.From(_segments[2]);
    
    public ResourceGroupIdentifier ResourceGroupIdentifier => ResourceGroupIdentifier.From(_segments[4]);

    public string ResourceName => _segments[^1];
}