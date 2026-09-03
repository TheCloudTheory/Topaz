using Topaz.Service.Shared.Domain;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal record EventGridTopicContext(string TopicName,
    SubscriptionIdentifier SubscriptionIdentifier,
    ResourceGroupIdentifier ResourceGroupIdentifier);