using Azure.Core;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Importer;

public sealed class ImportResult(bool dryRun) : TopazApiModel
{
    public bool DryRun { get; set; } = dryRun;

    public void AddSubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        Subscription = subscriptionIdentifier;
    }

    public SubscriptionIdentifier? Subscription { get; private set; }

    public void AddResourceGroup(ResourceGroupIdentifier resourceGroupIdentifier)
    {
        ResourceGroups.Add(resourceGroupIdentifier);
    }

    public IList<ResourceGroupIdentifier> ResourceGroups { get; set; } = [];

    public void Add(ResourceIdentifier resourceId)
    {
        Resources.Add(resourceId);
    }

    public IList<ResourceIdentifier> Resources { get; set; } = [];
}