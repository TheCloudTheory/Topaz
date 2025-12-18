using System.ComponentModel;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Topaz.AspNetCore.Extensions;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Helps in setting up and managing subscriptions in Topaz.")]
[UsedImplicitly]
public sealed class SubscriptionTool
{
    [McpServerTool]
    [Description("Creates a subscription in Topaz")]
    [UsedImplicitly]
    public static async Task CreateSubscription(
        ConfigurationBuilder builder,
        [Description("ID of the subscription to create.")]
        Guid subscriptionId,
        [Description("Name of the subscription to create.")]
        string subscriptionName)
    {
        await builder.AddTopaz(subscriptionId)
            .AddSubscription(subscriptionId, subscriptionName);
    }
}