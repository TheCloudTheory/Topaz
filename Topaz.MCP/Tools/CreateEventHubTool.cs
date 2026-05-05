using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates and manages Azure Event Hub resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateEventHubTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates an Event Hub namespace in the given resource group.")]
    [UsedImplicitly]
    public static async Task<EventHubNamespaceResult> CreateEventHubNamespace(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the Event Hub namespace to create.")]
        string namespaceName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var data = new EventHubsNamespaceData(new AzureLocation(location));
        await resourceGroup.Value.GetEventHubsNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, namespaceName, data)
            .ConfigureAwait(false);

        return new EventHubNamespaceResult
        {
            NamespaceName = namespaceName,
            ConnectionString = TopazResourceHelpers.GetEventHubConnectionString(namespaceName),
        };
    }

    [McpServerTool]
    [Description("Creates an Event Hub inside an existing Event Hub namespace.")]
    [UsedImplicitly]
    public static async Task<EventHubResult> CreateEventHub(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the Event Hub namespace.")]
        string namespaceName,
        [Description("Name of the Event Hub to create.")]
        string eventHubName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Number of partitions (1–32, default: 4).")]
        int partitionCount = 4,
        [Description("Message retention in days (1–7, default: 1).")]
        int messageRetentionInDays = 1)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);
        var @namespace = await resourceGroup.Value.GetEventHubsNamespaceAsync(namespaceName).ConfigureAwait(false);

        var hubData = new EventHubData
        {
            PartitionCount = partitionCount,
            RetentionDescription = new RetentionDescription
            {
                RetentionTimeInHours = messageRetentionInDays * 24L,
                CleanupPolicy = CleanupPolicyRetentionDescription.Delete,
            },
        };

        await @namespace.Value.GetEventHubs()
            .CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, hubData)
            .ConfigureAwait(false);

        return new EventHubResult
        {
            NamespaceName = namespaceName,
            EventHubName = eventHubName,
            PartitionCount = partitionCount,
            ConnectionString = TopazResourceHelpers.GetEventHubConnectionString(namespaceName),
        };
    }

    public sealed record EventHubNamespaceResult
    {
        public required string NamespaceName { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
    }

    public sealed record EventHubResult
    {
        public required string NamespaceName { [UsedImplicitly] get; init; }
        public required string EventHubName { [UsedImplicitly] get; init; }
        public required int PartitionCount { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
    }
}
