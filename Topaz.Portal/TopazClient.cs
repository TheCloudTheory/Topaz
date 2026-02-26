using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Topaz.Identity;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.Subscriptions;
using Topaz.ResourceManager;

namespace Topaz.Portal;

public class TopazClient
{
    private readonly ArmClient _armClient;

    public TopazClient()
    {
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        _armClient = new ArmClient(credentials, Guid.Empty.ToString(), TopazArmClientOptions.New);
    }

    public async Task<ListSubscriptionsResponse> ListSubscriptions()
    {
        var subscriptions = new List<SubscriptionResource>();

        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(subscription);
        }

        return new ListSubscriptionsResponse
        {
            Value = subscriptions.Select(sub => new SubscriptionDto
            {
                DisplayName = sub.Data.DisplayName,
                SubscriptionId = sub.Data.SubscriptionId,
                Id = sub.Id.ToString()
            }).ToArray()
        };
    }

    public async Task<ListResourceGroupsResponse> ListResourceGroups()
    {
        var subscriptions = await ListSubscriptions();
        var resourceGroups = new List<ResourceGroupResource>();

        foreach (var subscription in subscriptions.Value)
        {
            await foreach (var rg in _armClient
                               .GetSubscriptionResource(
                                   new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"))
                               .GetResourceGroups().GetAllAsync())
            {
                resourceGroups.Add(rg);
            }
        }
        
        return new ListResourceGroupsResponse
        {
            Value = resourceGroups.Select(rg => new ResourceGroupDto
            {
                Id = rg.Id.ToString(),
                Name = rg.Data.Name,
                Location = rg.Data.Location
            }).ToArray()
        };       
    }
}