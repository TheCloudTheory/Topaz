using System.ComponentModel;
using Azure.ResourceManager;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Helps in setting up and managing subscriptions in Topaz.")]
[UsedImplicitly]
public sealed class SubscriptionTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    
    [McpServerTool]
    [Description("Creates a subscription in Topaz")]
    [UsedImplicitly]
    public static async Task CreateSubscription(
        ConfigurationBuilder builder,
        [Description("ID of the subscription to create.")]
        Guid subscriptionId,
        [Description("Name of the subscription to create.")]
        string subscriptionName,
        [Description("Object ID of the user who will perform the operation. You can use empty GUID to indicate superadmin.")]
        string objectId)
    {
        await builder.AddTopaz(subscriptionId, objectId)
            .AddSubscription(subscriptionId, subscriptionName, new AzureLocalCredential(objectId));
    }
    
    [McpServerTool]
    [Description("List available subscriptions in Topaz")]
    [UsedImplicitly]
    public static async Task<List<Subscription>> ListSubscriptions(
        [Description("Object ID of the user who will perform the operation. You can use empty GUID to indicate superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, Guid.Empty.ToString(), ArmClientOptions);
        var subscriptions = new List<Subscription>();
        
        await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(new Subscription
            {
                SubscriptionId = subscription.Data.SubscriptionId,
                SubscriptionName = subscription.Data.DisplayName,
            });
        }
        
        return subscriptions;
    }

    public record Subscription
    {
        public required string SubscriptionId { [UsedImplicitly] get; init; }
        public required string SubscriptionName { [UsedImplicitly] get; init; }
    }
}