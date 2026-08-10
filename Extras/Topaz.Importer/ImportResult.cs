using Azure.Core;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Importer;

public sealed class ImportResult(bool dryRun) : TopazApiModel
{
    public bool DryRun { get; set; } = dryRun;

    public void AddSubscription(SubscriptionIdentifier subscriptionIdentifier)
    {
        Subscription = subscriptionIdentifier.Value.ToString();
    }

    public string? Subscription { get; private set; }

    public void AddResourceGroup(ResourceGroupIdentifier resourceGroupIdentifier)
    {
        ResourceGroups.Add(resourceGroupIdentifier.Value);
    }

    public IList<string> ResourceGroups { get; set; } = [];
    public uint TotalResourceGroups => (uint)ResourceGroups.Count;

    public void Add(ResourceIdentifier resourceId)
    {
        Resources.Add(resourceId.ToString());
    }

    public IList<string> Resources { get; set; } = [];
    public uint TotalResources => (uint)Resources.Count;
}