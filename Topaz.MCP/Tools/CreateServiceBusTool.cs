using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates and manages Azure Service Bus resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateServiceBusTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Service Bus namespace in the given resource group.")]
    [UsedImplicitly]
    public static async Task<ServiceBusNamespaceResult> CreateServiceBusNamespace(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the Service Bus namespace to create.")]
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

        var data = new ServiceBusNamespaceData(new AzureLocation(location));
        await resourceGroup.Value.GetServiceBusNamespaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, namespaceName, data)
            .ConfigureAwait(false);

        return new ServiceBusNamespaceResult
        {
            NamespaceName = namespaceName,
            ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(namespaceName),
            ConnectionStringWithTls = TopazResourceHelpers.GetServiceBusConnectionStringWithTls(namespaceName),
        };
    }

    [McpServerTool]
    [Description("Creates a queue inside an existing Service Bus namespace.")]
    [UsedImplicitly]
    public static async Task<ServiceBusEntityResult> CreateServiceBusQueue(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the Service Bus namespace.")]
        string namespaceName,
        [Description("Name of the queue to create.")]
        string queueName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Maximum delivery count before the message is dead-lettered (default: 10).")]
        int maxDeliveryCount = 10)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(namespaceName).ConfigureAwait(false);

        var queueData = new ServiceBusQueueData { MaxDeliveryCount = maxDeliveryCount };
        await @namespace.Value.GetServiceBusQueues()
            .CreateOrUpdateAsync(WaitUntil.Completed, queueName, queueData)
            .ConfigureAwait(false);

        return new ServiceBusEntityResult
        {
            NamespaceName = namespaceName,
            EntityName = queueName,
            EntityType = "Queue",
            ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(namespaceName),
        };
    }

    [McpServerTool]
    [Description("Creates a topic inside an existing Service Bus namespace.")]
    [UsedImplicitly]
    public static async Task<ServiceBusEntityResult> CreateServiceBusTopic(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the Service Bus namespace.")]
        string namespaceName,
        [Description("Name of the topic to create.")]
        string topicName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(namespaceName).ConfigureAwait(false);

        await @namespace.Value.GetServiceBusTopics()
            .CreateOrUpdateAsync(WaitUntil.Completed, topicName, new ServiceBusTopicData())
            .ConfigureAwait(false);

        return new ServiceBusEntityResult
        {
            NamespaceName = namespaceName,
            EntityName = topicName,
            EntityType = "Topic",
            ConnectionString = TopazResourceHelpers.GetServiceBusConnectionString(namespaceName),
        };
    }

    public sealed record ServiceBusNamespaceResult
    {
        public required string NamespaceName { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
        public required string ConnectionStringWithTls { [UsedImplicitly] get; init; }
    }

    public sealed record ServiceBusEntityResult
    {
        public required string NamespaceName { [UsedImplicitly] get; init; }
        public required string EntityName { [UsedImplicitly] get; init; }
        public required string EntityType { [UsedImplicitly] get; init; }
        public required string ConnectionString { [UsedImplicitly] get; init; }
    }
}
